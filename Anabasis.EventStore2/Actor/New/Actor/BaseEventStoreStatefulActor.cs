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
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseEventStoreStatefulActor<TAggregate> : BaseStatelessActor2, IStatefulActor2<TAggregate> where TAggregate : class, IAggregate, new()
    {

        private readonly object _catchUpSyncLock = new();

        private CatchupCacheSubscriptionHolder<TAggregate>[]? _catchupCacheSubscriptionHolders;

        private readonly ISnapshotStrategy? _snapshotStrategy;
        private readonly ISnapshotStore<TAggregate>? _snapshotStore;
        private readonly IConnectionStatusMonitor<IEventStoreConnection> _connectionMonitor;
        private readonly DateTime _lastProcessedEventUtcTimestamp;
        private readonly SourceCache<TAggregate, string> _cache;
        private readonly SourceCache<TAggregate, string> _caughtingUpCache;
        private readonly BehaviorSubject<bool> _isCaughtUpSubject;
        private readonly BehaviorSubject<bool> _isStaleSubject;
        protected readonly IAggregateCacheConfiguration<TAggregate> _catchupCacheConfiguration;
        private readonly IKillSwitch _killSwitch;

        public IEventTypeProvider<TAggregate> EventTypeProvider { get; }

        public bool UseSnapshot => _catchupCacheConfiguration.UseSnapshot;
        public IObservable<bool> OnStale => _isStaleSubject.AsObservable();
        public IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
        public bool IsStale => _isStaleSubject.Value;
        public override bool IsCaughtUp => _isCaughtUpSubject.Value;
        public override bool IsConnected => _connectionMonitor.IsConnected;

        public ICatchupCacheSubscriptionHolder[] GetSubscriptionStates()
        {
            if (null == _catchupCacheSubscriptionHolders) return Array.Empty<ICatchupCacheSubscriptionHolder>();

            return _catchupCacheSubscriptionHolders.ToArray();
        }

        public BaseEventStoreStatefulActor(
           IActorConfiguration actorConfiguration,
           IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
           IAggregateCacheConfiguration<TAggregate> catchupCacheConfiguration,
           IEventTypeProvider<TAggregate> eventTypeProvider,
           ILoggerFactory? loggerFactory = null,
           ISnapshotStore<TAggregate>? snapshotStore = null,
           ISnapshotStrategy? snapshotStrategy = null,
           IKillSwitch? killSwitch = null) : base(actorConfiguration, loggerFactory)
        {

            EventTypeProvider = eventTypeProvider;

            _killSwitch = killSwitch ?? new KillSwitch();

#nullable disable
            _cache = new SourceCache<TAggregate, string>(item => item.EntityId);
            _caughtingUpCache = new SourceCache<TAggregate, string>(item => item.EntityId);
#nullable enable

            _catchupCacheConfiguration = catchupCacheConfiguration;
            _connectionMonitor = connectionMonitor;
            _snapshotStrategy = snapshotStrategy;
            _snapshotStore = snapshotStore;
            _lastProcessedEventUtcTimestamp = DateTime.MinValue;
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
            _isStaleSubject = new BehaviorSubject<bool>(true);

            if (UseSnapshot && (_snapshotStore == null && _snapshotStrategy != null || _snapshotStore != null && _snapshotStrategy == null))
            {
                throw new InvalidOperationException($"Snapshots are activated on {GetType().Name}. To use snapshots both a snapshotStore and a snapshotStrategy are required " +
                    $"[snapshotStore is null = {snapshotStore == null}, snapshotStrategy is null = {snapshotStrategy == null}]");
            }
        }

        protected void Initialize()
        {
            var catchupCacheSubscriptionHolders = new[] { new CatchupCacheSubscriptionHolder<TAggregate>(_catchupCacheConfiguration.CrashAppIfSubscriptionFail) };

            Initialize(catchupCacheSubscriptionHolders);
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

                AddToCleanup(subscription);

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

            AddToCleanup(isStaleSubscription);
        }

        protected abstract Task OnLoadSnapshot(
            CatchupCacheSubscriptionHolder<TAggregate>[]? catchupCacheSubscriptionHolders,
            ISnapshotStrategy? snapshotStrategy,
            ISnapshotStore<TAggregate>? snapshotStore);

        public IObservableCache<TAggregate, string> AsObservableCache()
        {
            return _cache.AsObservableCache();
        }

        protected string?[] GetEventsFilters()
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

#nullable disable

            var entry = CurrentCache.Lookup(@event.EntityId);

#nullable enable

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

        public async Task ConnectToEventStream()
        {
   
            await OnLoadSnapshot(_catchupCacheSubscriptionHolders, _snapshotStrategy, _snapshotStore);

            if (null == _catchupCacheSubscriptionHolders)
                throw new ArgumentNullException("_catchupCacheSubscriptionHolders");

            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
            {
                if (null != catchupCacheSubscriptionHolder.EventStreamConnectionDisposable) 
                    catchupCacheSubscriptionHolder.EventStreamConnectionDisposable.Dispose();

                catchupCacheSubscriptionHolder.EventStreamConnectionDisposable = ConnectToEventStream(_connectionMonitor.Connection, catchupCacheSubscriptionHolder);

                AddToCleanup(catchupCacheSubscriptionHolder.EventStreamConnectionDisposable);

            }

        }

        public Task Disconnect()
        {
            if (null == _catchupCacheSubscriptionHolders)
                throw new ArgumentNullException("_catchupCacheSubscriptionHolders");

            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
            {
                catchupCacheSubscriptionHolder?.EventStreamConnectionDisposable?.Dispose();
            }

            return Task.CompletedTask;
        }

        public TAggregate? GetCurrent(string key)
        {
            return _cache.Items.FirstOrDefault(item => item.EntityId == key);
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
#nullable disable
                            if (_snapshotStrategy.IsSnapshotRequired(aggregate))
                            {
                                Logger?.LogInformation($"{Id} => Snapshoting aggregate => {aggregate.EntityId} {aggregate.GetType()} - v.{aggregate.Version}");

                                var eventFilter = GetEventsFilters();

                                aggregate.VersionFromSnapshot = aggregate.Version;

                                _snapshotStore.Save(eventFilter, aggregate).Wait();

                            }
#nullable enable
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
                            _killSwitch.KillProcess(exception);
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

#nullable disable

            var aggregateEvent = _catchupCacheConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IAggregateEvent<TAggregate>;
            aggregateEvent.EventNumber = recordedEvent.EventNumber;

#nullable enable

            return aggregateEvent;

        }

    }
}
