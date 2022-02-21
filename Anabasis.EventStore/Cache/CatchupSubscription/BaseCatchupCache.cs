using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseCatchupCache< TAggregate> : IEventStoreCache<TAggregate> where TAggregate : IAggregate, new()
    {

        private readonly object _catchUpLocker = new();
        private IDisposable _eventStoreConnectionStatus;
        private CatchupCacheSubscriptionHolder< TAggregate>[] _catchupCacheSubscriptionHolders;

        private readonly ISnapshotStrategy _snapshotStrategy;
        private readonly ISnapshotStore< TAggregate> _snapshotStore;
        private readonly IConnectionStatusMonitor _connectionMonitor;
        private readonly DateTime _lastProcessedEventUtcTimestamp;
        private readonly CompositeDisposable _cleanUp;
        private readonly SourceCache<TAggregate, string> _cache;
        private readonly SourceCache<TAggregate, string> _caughtingUpCache;
        private readonly BehaviorSubject<bool> _connectionStatusSubject;
        private readonly BehaviorSubject<bool> _isCaughtUpSubject;
        private readonly BehaviorSubject<bool> _isStaleSubject;
        private readonly IEventStoreCacheConfiguration< TAggregate> _catchupCacheConfiguration;

        protected Microsoft.Extensions.Logging.ILogger Logger { get; }

        public IEventTypeProvider< TAggregate> EventTypeProvider { get; }
        public string Id { get; }
        public bool IsWiredUp { get; private set; }
        public bool UseSnapshot { get; private set; }
        public IObservable<bool> OnStale => _isStaleSubject.AsObservable();
        public IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
        public bool IsStale => _isStaleSubject.Value;
        public bool IsCaughtUp => _isCaughtUpSubject.Value;
        public bool IsConnected => _connectionMonitor.IsConnected && IsWiredUp;
        public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;

        public ICatchupCacheSubscriptionHolder[] GetSubscriptionStates()
        {
            return _catchupCacheSubscriptionHolders.ToArray();
        }

        public BaseCatchupCache(
           IConnectionStatusMonitor connectionMonitor,
           IEventStoreCacheConfiguration< TAggregate> catchupCacheConfiguration,
           IEventTypeProvider< TAggregate> eventTypeProvider,
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

            IsWiredUp = false;
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
            _connectionStatusSubject = new BehaviorSubject<bool>(false);
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
            _isStaleSubject = new BehaviorSubject<bool>(true);
            _cleanUp = new CompositeDisposable();
 
        }

        protected void Initialize()
        {
            _catchupCacheSubscriptionHolders = new[] { new CatchupCacheSubscriptionHolder< TAggregate>() };

            Initialize(_catchupCacheSubscriptionHolders);
        }

        protected void Initialize(CatchupCacheSubscriptionHolder< TAggregate>[] catchupCacheSubscriptionHolders)
        {

            _catchupCacheSubscriptionHolders = catchupCacheSubscriptionHolders;

            foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
            {
                var subscription = catchupCacheSubscriptionHolder.OnCaughtUp.Subscribe(hasStreamSubscriptionCaughtUp =>
                {

                    lock (_catchUpLocker)
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
            CatchupCacheSubscriptionHolder< TAggregate>[] catchupCacheSubscriptionHolders,
            ISnapshotStrategy snapshotStrategy, 
            ISnapshotStore< TAggregate> snapshotStore);

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

        protected void OnResolvedEvent(ResolvedEvent @event)
        {
            Logger?.LogDebug($"{Id} => OnResolvedEvent {@event.Event.EventType} - v.{@event.Event.EventNumber} - IsCaughtUp => {IsCaughtUp}");

            UpdateCacheState(@event, CurrentCache);
        }

        public void Connect()
        {
            if (IsWiredUp) return;

            Logger?.LogDebug($"{Id} => Connecting");

            IsWiredUp = true;

            _eventStoreConnectionStatus = _connectionMonitor.GetEvenStoreConnectionStatus().Subscribe(async connectionChanged =>
            {
                Logger?.LogDebug($"{Id} => IsConnected: {connectionChanged.IsConnected}");

                _connectionStatusSubject.OnNext(connectionChanged.IsConnected);

                if (connectionChanged.IsConnected)
                {

                    await OnLoadSnapshot(_catchupCacheSubscriptionHolders, _snapshotStrategy, _snapshotStore);

                    foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
                    {
                        catchupCacheSubscriptionHolder.EventStreamConnectionDisposable = ConnectToEventStream(connectionChanged.Value, catchupCacheSubscriptionHolder)
                          .Subscribe(@event =>
                          {
                              OnResolvedEvent(@event);

                              if (IsStale) _isStaleSubject.OnNext(false);

                              catchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = @event.Event.EventNumber;
                              catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

                          });
                    }
                }
                else
                {
                    foreach (var catchupCacheSubscriptionHolder in _catchupCacheSubscriptionHolders)
                    {

                        if (null != catchupCacheSubscriptionHolder.EventStreamConnectionDisposable)
                            catchupCacheSubscriptionHolder.EventStreamConnectionDisposable.Dispose();
                    }

                    if (IsCaughtUp)
                    {
                        _isCaughtUpSubject.OnNext(false);
                    }
                }
            });

            _cleanUp.Add(_eventStoreConnectionStatus);
        }

        public TAggregate GetCurrent(string key)
        {
            return _cache.Items.FirstOrDefault(item => item.EntityId.Equals(key));
        }

        public TAggregate[] GetCurrents()
        {
            return _cache.Items.ToArray();
        }

        private IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection,
            CatchupCacheSubscriptionHolder< TAggregate> catchupCacheSubscriptionHolder)
        {

            return Observable.Create<ResolvedEvent>(obs =>
            {

                Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent @event)
                {
                    lock (_catchUpLocker)
                    {

                        Logger?.LogDebug($"{Id} => OnEvent {@event.Event.EventType} - v.{@event.Event.EventNumber}");

                        obs.OnNext(@event);

                        if (IsCaughtUp && UseSnapshot)
                        {
                            foreach (var aggregate in _cache.Items)
                            {
                                if (_snapshotStrategy.IsSnapShotRequired(aggregate))
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

                void onSubscriptionDropped(EventStoreCatchUpSubscription _, SubscriptionDropReason subscriptionDropReason, Exception exception)
                {
                    switch (subscriptionDropReason)
                    {
                        case SubscriptionDropReason.UserInitiated:
                        case SubscriptionDropReason.ConnectionClosed:
                            break;

                        case SubscriptionDropReason.CatchUpError:
                            Logger?.LogInformation($"{nameof(SubscriptionDropReason)} - {subscriptionDropReason}", exception);
                            break;

                        case SubscriptionDropReason.NotAuthenticated:
                        case SubscriptionDropReason.AccessDenied:
                        case SubscriptionDropReason.SubscribingError:
                        case SubscriptionDropReason.ServerError:
                        case SubscriptionDropReason.ProcessingQueueOverflow:
                        case SubscriptionDropReason.EventHandlerException:
                        case SubscriptionDropReason.MaxSubscribersReached:
                        case SubscriptionDropReason.PersistentSubscriptionDeleted:
                        case SubscriptionDropReason.Unknown:
                        case SubscriptionDropReason.NotFound:
                            throw new InvalidOperationException($"{nameof(SubscriptionDropReason)} {subscriptionDropReason} throwed the consumer in a invalid state", exception);

                        default:
                            throw new InvalidOperationException($"{nameof(SubscriptionDropReason)} {subscriptionDropReason} not found", exception);
                    }
                }

                var subscription = GetEventStoreCatchUpSubscription(catchupCacheSubscriptionHolder,
                    connection, onEvent, onSubscriptionDropped);

                return Disposable.Create(() =>
                {
                    subscription.Stop();
                });

            });
        }

        protected abstract EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(
            CatchupCacheSubscriptionHolder< TAggregate> catchupCacheSubscriptionHolder,
            IEventStoreConnection connection,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped);

        private IAggregateEvent<TAggregate> DeserializeEvent(RecordedEvent recordedEvent)
        {
            var targetType = EventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            if (null == targetType) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");

            return _catchupCacheConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IAggregateEvent<TAggregate>;
        }

        protected void UpdateCacheState(ResolvedEvent resolvedEvent, SourceCache<TAggregate, string> specificCache = null)
        {
            var recordedEvent = resolvedEvent.Event;

            Logger?.LogDebug($"{Id} => UpdateCacheState: {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} - v.{resolvedEvent.Event.EventNumber}");

            var cache = specificCache ?? _cache;

            var @event = DeserializeEvent(recordedEvent);

            if (null == @event)
            {
                throw new EventNotSupportedException(recordedEvent);
            }

            //we do not reprocess commands 
            if (!IsCaughtUp && @event.IsCommand) return;

            var entry = cache.Lookup(@event.EntityId);

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

            cache.AddOrUpdate(entity);

        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}
