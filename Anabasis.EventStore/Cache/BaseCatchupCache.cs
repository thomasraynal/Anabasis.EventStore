using Anabasis.Common;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseCatchupCache<TAggregate> : IAggregateCache<TAggregate> where TAggregate : class, IAggregate, new()
    {

        private readonly object _catchUpSyncLock = new();

        private CatchupCacheSubscriptionHolder<TAggregate>[] _catchupCacheSubscriptionHolders;

        private readonly ISnapshotStrategy _snapshotStrategy;
        private readonly ISnapshotStore<TAggregate> _snapshotStore;
        private readonly IConnectionStatusMonitor<IEventStoreConnection> _connectionMonitor;
        private readonly DateTime _lastProcessedEventUtcTimestamp;
        private readonly CompositeDisposable _cleanUp;
        private readonly SourceCache<TAggregate, string> _cache;
        private readonly SourceCache<TAggregate, string> _caughtingUpCache;
        private readonly BehaviorSubject<bool> _isCaughtUpSubject;
        private readonly BehaviorSubject<bool> _isStaleSubject;
        private readonly IAggregateCacheConfiguration<TAggregate> _catchupCacheConfiguration;

        protected Microsoft.Extensions.Logging.ILogger Logger { get; }

        public IEventTypeProvider<TAggregate> EventTypeProvider { get; }
        public string Id { get; }
        public bool UseSnapshot { get; private set; }
        public IObservable<bool> OnStale => _isStaleSubject.AsObservable();
        public IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
        public bool IsStale => _isStaleSubject.Value;
        public bool IsCaughtUp => _isCaughtUpSubject.Value;
        public bool IsConnected => _connectionMonitor.IsConnected;

        public ICatchupCacheSubscriptionHolder[] GetSubscriptionStates()
        {
            return _catchupCacheSubscriptionHolders.ToArray();
        }

        public BaseCatchupCache(
           IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
           IAggregateCacheConfiguration<TAggregate> catchupCacheConfiguration,
           IEventTypeProvider<TAggregate> eventTypeProvider,
           ILoggerFactory loggerFactory,
           ISnapshotStore<TAggregate> snapshotStore = null,
           ISnapshotStrategy snapshotStrategy = null)
        {

            if (snapshotStore == null && snapshotStrategy != null || snapshotStore != null && snapshotStrategy == null)
            {
                throw new InvalidOperationException($"To use snapshots both a snapshotStore and a snapshotStrategy are required " +
                    $"[snapshotStore is null = {snapshotStore == null}, snapshotStrategy is null ={snapshotStrategy == null}]");
            }

            UseSnapshot = snapshotStore != null && snapshotStrategy != null;

            Id = $"{GetType()}-{Guid.NewGuid()}";
            EventTypeProvider = eventTypeProvider;

            Logger = loggerFactory?.CreateLogger(this.GetType());

            _cache = new SourceCache<TAggregate, string>(item => item.EntityId);
            _caughtingUpCache = new SourceCache<TAggregate, string>(item => item.EntityId);
            _catchupCacheConfiguration = catchupCacheConfiguration;
            _connectionMonitor = connectionMonitor;
            _snapshotStrategy = snapshotStrategy;
            _snapshotStore = snapshotStore;
            _lastProcessedEventUtcTimestamp = DateTime.MinValue;
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
            _isStaleSubject = new BehaviorSubject<bool>(true);
            _cleanUp = new CompositeDisposable();

        }

        protected void Initialize()
        {
            _catchupCacheSubscriptionHolders = new[] { new CatchupCacheSubscriptionHolder<TAggregate>(_catchupCacheConfiguration.DoAppCrashIfSubscriptionFail) };

            Initialize(_catchupCacheSubscriptionHolders);
        }

        protected void Initialize(CatchupCacheSubscriptionHolder<TAggregate>[] catchupCacheSubscriptionHolders)
        {

            _catchupCacheSubscriptionHolders = catchupCacheSubscriptionHolders;

            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
            {
                var subscription = catchupCacheSubscriptionHolder.OnCaughtUp.Subscribe(hasStreamSubscriptionCaughtUp =>
                {

                    lock (_catchUpSyncLock)
                    {

                        if (hasStreamSubscriptionCaughtUp && !IsCaughtUp)
                        {
                            if (_catchupCacheSubscriptionHolders.All(holder => holder.IsCaughtUp))
                            {

                                Logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");

                                if (!IsCaughtUp)
                                {

                                    _cache.Edit(innerCache =>
                                    {
                                        Logger?.LogInformation($"{Id} => OnCaughtUp - switch from CaughtingUpCache");

                                        innerCache.Load(_caughtingUpCache.Items);

                                        _caughtingUpCache.Clear();

                                    });

                                    _isCaughtUpSubject.OnNext(true);

                                }

                                Logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");
                            }
                        }
                        else if (!hasStreamSubscriptionCaughtUp && IsCaughtUp)
                        {
                            _isCaughtUpSubject.OnNext(false);
                        }
                    }

                });

                _cleanUp.Add(subscription);

            }

            var isStaleSubscription = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
                {
                    if (DateTime.UtcNow >
                        catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp.Add(_catchupCacheConfiguration.IsStaleTimeSpan))
                    {
                        _isStaleSubject.OnNext(true);
                        break;
                    }
                }

            });

            _cleanUp.Add(isStaleSubscription);
        }

        protected abstract Task OnLoadSnapshot(
            CatchupCacheSubscriptionHolder<TAggregate>[] catchupCacheSubscriptionHolders,
            ISnapshotStrategy snapshotStrategy,
            ISnapshotStore<TAggregate> snapshotStore);

        public IObservableCache<TAggregate, string> AsObservableCache()
        {
            return _cache.AsObservableCache();
        }

        protected string[] GetEventsFilters()
        {
            var eventTypeFilters = EventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

            return eventTypeFilters;
        }

        protected SourceCache<TAggregate, string> CurrentCache => IsCaughtUp ? _cache : _caughtingUpCache;

        protected void OnResolvedEvent(ResolvedEvent resolvedEvent)
        {
            var recordedEvent = resolvedEvent.Event;

            Logger?.LogDebug($"{Id} => OnResolvedEvent: {recordedEvent.EventId} {recordedEvent.EventStreamId} - v.{recordedEvent.EventNumber}");

            var @event = DeserializeEvent(recordedEvent);

            if (null == @event)
            {
                throw new EventNotSupportedException(recordedEvent);
            }

            //we do not reprocess commands 
            if (!IsCaughtUp && @event.IsCommand) return;

            var entry = CurrentCache.Lookup(@event.EntityId);

            TAggregate entity;

            if (entry.HasValue)
            {
                entity = entry.Value;

                if (entity.Version == recordedEvent.EventNumber)
                {
                    return;
                }
            }
            else
            {
                Logger?.LogDebug($"{Id} => Creating aggregate: {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} - v.{resolvedEvent.Event.EventNumber}");

                entity = new TAggregate();
                entity.SetEntityId(@event.EntityId);
            }

            Logger?.LogDebug($"{Id} => Updating aggregate: {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} - v.{resolvedEvent.Event.EventNumber}");

            entity.ApplyEvent(@event, false, _catchupCacheConfiguration.KeepAppliedEventsOnAggregate);

            CurrentCache.AddOrUpdate(entity);

        }

        public async Task Connect()
        {
   
            await OnLoadSnapshot(_catchupCacheSubscriptionHolders, _snapshotStrategy, _snapshotStore);

            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
            {
                if (null != catchupCacheSubscriptionHolder.EventStreamConnectionDisposable) 
                    catchupCacheSubscriptionHolder.EventStreamConnectionDisposable.Dispose();

                catchupCacheSubscriptionHolder.EventStreamConnectionDisposable = ConnectToEventStream(_connectionMonitor.Connection, catchupCacheSubscriptionHolder);

                _cleanUp.Add(catchupCacheSubscriptionHolder.EventStreamConnectionDisposable);

            }

        }

        public Task Disconnect()
        {
        
            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
            {
                catchupCacheSubscriptionHolder.EventStreamConnectionDisposable.Dispose();
            }

            return Task.CompletedTask;
        }

        public TAggregate GetCurrent(string key)
        {
            return _cache.Items.FirstOrDefault(item => item.EntityId.Equals(key));
        }

        public TAggregate[] GetCurrents()
        {
            return _cache.Items.ToArray();
        }

        private IDisposable ConnectToEventStream(IEventStoreConnection connection, CatchupCacheSubscriptionHolder<TAggregate> catchupCacheSubscriptionHolder)
        {

            void stopSubscription()
            {
                if (null != catchupCacheSubscriptionHolder.EventStoreCatchUpSubscription)
                {
                    _isCaughtUpSubject.OnNext(false);
                    catchupCacheSubscriptionHolder.EventStoreCatchUpSubscription.Stop();
                }
               
            }

            void createNewCatchupSubscription()
            {
                stopSubscription();

                catchupCacheSubscriptionHolder.EventStoreCatchUpSubscription = GetEventStoreCatchUpSubscription(catchupCacheSubscriptionHolder, connection, onEvent, onSubscriptionDropped);
            }

            Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent @event)
            {

                lock (_catchUpSyncLock)
                {

                    Logger?.LogDebug($"{Id} => OnEvent {@event.Event.EventType} - v.{@event.Event.EventNumber}");

                    OnResolvedEvent(@event);

                    if (IsStale) _isStaleSubject.OnNext(false);

                    catchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = @event.Event.EventNumber;
                    catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

                    if (IsCaughtUp && UseSnapshot)
                    {
                        foreach (var aggregate in _cache.Items)
                        {
                            if (_snapshotStrategy.IsSnapshotRequired(aggregate))
                            {
                                Logger?.LogInformation($"{Id} => Snapshoting aggregate => {aggregate.EntityId} {aggregate.GetType()} - v.{aggregate.Version}");

                                var eventFilter = GetEventsFilters();

                                aggregate.VersionFromSnapshot = aggregate.Version;

                                _snapshotStore.Save(eventFilter, aggregate).Wait();

                            }
                        }
                    }
                }

                return Task.CompletedTask;

            }

            void onSubscriptionDropped(EventStoreCatchUpSubscription eventStoreCatchUpSubscription, SubscriptionDropReason subscriptionDropReason, Exception exception)
            {

                switch (subscriptionDropReason)
                {

                    case SubscriptionDropReason.UserInitiated:
                        break;

                    case SubscriptionDropReason.ConnectionClosed:
                    case SubscriptionDropReason.EventHandlerException:
                    case SubscriptionDropReason.CatchUpError:
                    case SubscriptionDropReason.NotAuthenticated:
                    case SubscriptionDropReason.AccessDenied:
                    case SubscriptionDropReason.SubscribingError:
                    case SubscriptionDropReason.ServerError:
                    case SubscriptionDropReason.ProcessingQueueOverflow:
                    case SubscriptionDropReason.MaxSubscribersReached:
                    case SubscriptionDropReason.PersistentSubscriptionDeleted:
                    case SubscriptionDropReason.Unknown:
                    case SubscriptionDropReason.NotFound:
                    default:

                        Logger?.LogError(exception, $"{nameof(SubscriptionDropReason)}: [{subscriptionDropReason}] throwed the consumer in an invalid state");

                        if (catchupCacheSubscriptionHolder.CrashAppIfSubscriptionFail)
                        {
                            ExceptionDispatchInfo.Capture(exception).Throw();
                        }
                        else
                        {
                            Task.Delay(200).Wait();

                            createNewCatchupSubscription();
                        }

                        break;
                }
            }

            createNewCatchupSubscription();

            return Disposable.Create(() =>
            {
                stopSubscription();
            });

        }

        protected abstract EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(
            CatchupCacheSubscriptionHolder<TAggregate> catchupCacheSubscriptionHolder,
            IEventStoreConnection connection,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped);

        private IAggregateEvent<TAggregate> DeserializeEvent(RecordedEvent recordedEvent)
        {
            var targetType = EventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            if (null == targetType) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");

            return _catchupCacheConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IAggregateEvent<TAggregate>;
        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}
