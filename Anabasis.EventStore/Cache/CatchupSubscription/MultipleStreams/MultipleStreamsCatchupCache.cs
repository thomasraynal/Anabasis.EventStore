using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public class MultipleStreamsCatchupCacheSubscriptionHolder
    {
        private BehaviorSubject<bool> _isCaughtUpSubject;

        public MultipleStreamsCatchupCacheSubscriptionHolder()
        {
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
        }

        public bool IsCaughtUp => _isCaughtUpSubject.Value;
        public IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
 
        public string StreamId { get; internal set; }
        public DateTime LastProcessedEventUtcTimestamp { get; internal set; }
        public IDisposable EventStreamConnectionDisposable { get; internal set; }
        public long? LastProcessedEventSequenceNumber { get; internal set; } = null;
    }

    public class MultipleStreamsCatchupCache<TKey, TAggregate> : IEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        private readonly MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> _multipleStreamsCatchupCacheConfiguration;
        private readonly IEventTypeProvider<TKey, TAggregate> _eventTypeProvider;
        private readonly ISnapshotStrategy<TKey> _snapshotStrategy;
        private readonly ISnapshotStore<TKey, TAggregate> _snapshotStore;
        private readonly ManualResetEventSlim _blockEventConsumption;
        private readonly IConnectionStatusMonitor _connectionMonitor;
        private readonly IDisposable _isStaleDisposable;
        private readonly DateTime _lastProcessedEventUtcTimestamp;

        private ILogger<MultipleStreamsCatchupCache<TKey, TAggregate>> _logger { get; }
        private IDisposable _eventStoreConnectionStatus;
 
        private SourceCache<TAggregate, TKey> _cache { get; }
        private SourceCache<TAggregate, TKey> _caughtingUpCache { get; }
        private BehaviorSubject<bool> _connectionStatusSubject { get; }
        private BehaviorSubject<bool> _isStaleSubject { get; }

        private readonly List<MultipleStreamsCatchupCacheSubscriptionHolder> _multipleStreamsCatchupCacheSubscriptionHolders;

        public string Id { get; }
        public bool IsWiredUp { get; private set; }
        public IObservable<bool> OnStale => _isStaleSubject.AsObservable();
        public IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
        public bool IsStale => _isStaleSubject.Value;
        public bool IsCaughtUp => _isCaughtUpSubject.Value;
        public bool IsConnected => _connectionMonitor.IsConnected && IsWiredUp;
        public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;

        public MultipleStreamsCatchupCache(IConnectionStatusMonitor connectionMonitor,
           MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> multipleStreamsCatchupCacheConfiguration,
           IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
           ILoggerFactory loggerFactory,
           ISnapshotStore<TKey, TAggregate> snapshotStore = null,
           ISnapshotStrategy<TKey> snapshotStrategy = null)
        {

            IsWiredUp = false;
            Id = $"{GetType()}-{Guid.NewGuid()}";

            _cache = new SourceCache<TAggregate, TKey>(item => item.EntityId);
            _caughtingUpCache = new SourceCache<TAggregate, TKey>(item => item.EntityId);
            _blockEventConsumption = new ManualResetEventSlim();
            _logger = loggerFactory?.CreateLogger<MultipleStreamsCatchupCache<TKey, TAggregate>>();

            _multipleStreamsCatchupCacheSubscriptionHolders = multipleStreamsCatchupCacheConfiguration.StreamIds.Select(streamId =>
            {
                return new MultipleStreamsCatchupCacheSubscriptionHolder()
                {
                    StreamId = streamId
                };

            }).ToList();
            
            _multipleStreamsCatchupCacheConfiguration = multipleStreamsCatchupCacheConfiguration;
            _eventTypeProvider = eventTypeProvider;
            _connectionMonitor = connectionMonitor;
            _snapshotStrategy = snapshotStrategy;
            _snapshotStore = snapshotStore;
            _lastProcessedEventUtcTimestamp = DateTime.MinValue;

            _connectionStatusSubject = new BehaviorSubject<bool>(false);
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
            _isStaleSubject = new BehaviorSubject<bool>(true);

            _isStaleDisposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                foreach(var multipleStreamsCatchupCacheSubscriptionHolder in _multipleStreamsCatchupCacheSubscriptionHolders)
                {
                    if (DateTime.UtcNow > 
                        multipleStreamsCatchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp.Add(_multipleStreamsCatchupCacheConfiguration.IsStaleTimeSpan))
                    {
                        _isStaleSubject.OnNext(true);
                        break;
                    }
                }

            });
        }

        public IObservableCache<TAggregate, TKey> AsObservableCache()
        {
            return _cache.AsObservableCache();
        }

        public string[] GetEventsFilters()
        {
            var eventTypeFilters = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

            return eventTypeFilters;
        }

        protected async Task OnLoadSnapshot()
        {
            if (_multipleStreamsCatchupCacheConfiguration.UseSnapshot)
            {
                var eventTypeFilter = GetEventsFilters();

                var snapshots = await _snapshotStore.Get(eventTypeFilter);

                if (null != snapshots)
                {
                    foreach (var snapshot in snapshots)
                    {
                        _logger?.LogInformation($"{Id} => OnLoadSnapshot - EntityId: {snapshot.EntityId} StreamId: {snapshot.StreamId}");

                        _cache.AddOrUpdate(snapshot);
                    }

                }
            }
        }

        protected void OnResolvedEvent(ResolvedEvent @event)
        {
            var cache = IsCaughtUp ? _cache : _caughtingUpCache;

            _logger?.LogDebug($"{Id} => OnResolvedEvent {@event.Event.EventType} - v.{@event.Event.EventNumber} - IsCaughtUp => {IsCaughtUp}");

            UpdateCacheState(@event, cache);
        }

        public void Connect()
        {
            if (IsWiredUp) return;

            _logger?.LogDebug($"{Id} => Connecting");

            IsWiredUp = true;

            _eventStoreConnectionStatus = _connectionMonitor.GetEvenStoreConnectionStatus().Subscribe(async connectionChanged =>
            {
                _logger?.LogDebug($"{Id} => IsConnected: {connectionChanged.IsConnected}");

                _connectionStatusSubject.OnNext(connectionChanged.IsConnected);

                if (connectionChanged.IsConnected)
                {

                    await OnLoadSnapshot();

                    foreach (var multipleStreamsCatchupCacheSubscriptionHolder in _multipleStreamsCatchupCacheSubscriptionHolders)
                    {
                        multipleStreamsCatchupCacheSubscriptionHolder.EventStreamConnectionDisposable = ConnectToEventStream(connectionChanged.Value, multipleStreamsCatchupCacheSubscriptionHolder)
                          .Subscribe(@event =>
                          {
                              OnResolvedEvent(@event);

                              if (IsStale) _isStaleSubject.OnNext(false);

                              multipleStreamsCatchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = @event.Event.EventNumber;
                              multipleStreamsCatchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

                          });
                    }
                }
                else
                {
                    foreach (var multipleStreamsCatchupCacheSubscriptionHolder in _multipleStreamsCatchupCacheSubscriptionHolders)
                    {

                        if (null != multipleStreamsCatchupCacheSubscriptionHolder.EventStreamConnectionDisposable) 
                            multipleStreamsCatchupCacheSubscriptionHolder.EventStreamConnectionDisposable.Dispose();
                    }

                    if (IsCaughtUp)
                    {
                        _isCaughtUpSubject.OnNext(false);
                    }
                }
            });
        }

        public TAggregate GetCurrent(TKey key)
        {
            return _cache.Items.FirstOrDefault(item => item.EntityId.Equals(key));
        }

        public TAggregate[] GetCurrents()
        {
            return _cache.Items.ToArray();
        }

        public IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection,
            MultipleStreamsCatchupCacheSubscriptionHolder multipleStreamsCatchupCacheSubscriptionHolder)
        {

            return Observable.Create<ResolvedEvent>(obs =>
            {

                async Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent @event)
                {
                    _blockEventConsumption.Wait();

                    _logger?.LogDebug($"{Id} => OnEvent {@event.Event.EventType} - v.{@event.Event.EventNumber}");

                    obs.OnNext(@event);

                    if (_multipleStreamsCatchupCacheConfiguration.UseSnapshot)
                    {
                        foreach (var aggregate in _cache.Items)
                        {
                            if (_snapshotStrategy.IsSnapShotRequired(aggregate))
                            {
                                _logger?.LogInformation($"{Id} => Snapshoting aggregate => {aggregate.EntityId} {aggregate.GetType()} - v.{aggregate.Version}");

                                var eventFilter = GetEventsFilters();

                                aggregate.VersionSnapshot = aggregate.Version;

                                await _snapshotStore.Save(eventFilter, aggregate);

                            }
                        }
                    }
                }

                void onCaughtUp(EventStoreCatchUpSubscription _)
                {
                    _logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");

                    //event consumption should be sequential caughtUpEnd -> next event, so this is just to be sure...
                    _blockEventConsumption.Reset();

                    //this handle a caughting up NOT due to disconnection, i.e a caughting up due to a lag
                    if (!IsCaughtUp)
                    {

                        _cache.Edit(innerCache =>
                        {
                            _logger?.LogInformation($"{Id} => OnCaughtUp - switch from CaughtingUpCache");

                            innerCache.Load(_caughtingUpCache.Items);

                            _caughtingUpCache.Clear();

                        });

                        _isCaughtUpSubject.OnNext(true);

                    }

                    _logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");

                    _blockEventConsumption.Set();

                }

                void onSubscriptionDropped(EventStoreCatchUpSubscription _, SubscriptionDropReason subscriptionDropReason, Exception exception)
                {
                    switch (subscriptionDropReason)
                    {
                        case SubscriptionDropReason.UserInitiated:
                        case SubscriptionDropReason.ConnectionClosed:
                            break;

                        case SubscriptionDropReason.CatchUpError:
                            _logger?.LogInformation($"{nameof(SubscriptionDropReason)} - {subscriptionDropReason}", exception);
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

                var subscription = GetEventStoreCatchUpSubscription(multipleStreamsCatchupCacheSubscriptionHolder.StreamId,
                    multipleStreamsCatchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber,
                    connection, onEvent, onCaughtUp, onSubscriptionDropped);

                return Disposable.Create(() =>
                {
                    subscription.Stop();
                });

            });
        }

        private EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(
            string streamId,
            long? lastProcessedEventSequenceNumber,
            IEventStoreConnection connection,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent,
            Action<EventStoreCatchUpSubscription> onCaughtUp,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped)
        {

            void onCaughtUpWithCheckpoint(EventStoreCatchUpSubscription _)
            {
                _isCaughtUpSubject.OnNext(true);
            }

            _logger?.LogInformation($"{Id} => GetEventStoreCatchUpSubscription - SubscribeToStreamFrom {streamId} v.{lastProcessedEventSequenceNumber}]");

            var subscription = connection.SubscribeToStreamFrom(
              streamId,
              lastProcessedEventSequenceNumber,
              _multipleStreamsCatchupCacheConfiguration.CatchUpSubscriptionFilteredSettings,
              onEvent,
              onCaughtUpWithCheckpoint,
              onSubscriptionDropped,
              userCredentials: _multipleStreamsCatchupCacheConfiguration.UserCredentials);

            return subscription;
        }

        private IMutation<TKey, TAggregate> DeserializeEvent(RecordedEvent recordedEvent)
        {
            var targetType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            if (null == targetType) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");

            return _multipleStreamsCatchupCacheConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IMutation<TKey, TAggregate>;
        }

        protected void UpdateCacheState(ResolvedEvent resolvedEvent, SourceCache<TAggregate, TKey> specificCache = null)
        {
            var recordedEvent = resolvedEvent.Event;

            _logger?.LogDebug($"{Id} => UpdateCacheState: {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} - v.{resolvedEvent.Event.EventNumber}");

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
                _logger?.LogDebug($"{Id} => Creating aggregate: {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} - v.{resolvedEvent.Event.EventNumber}");

                entity = new TAggregate();
            }

            _logger?.LogDebug($"{Id} => Updating aggregate: {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} - v.{resolvedEvent.Event.EventNumber}");

            entity.ApplyEvent(@event, false, _multipleStreamsCatchupCacheConfiguration.KeepAppliedEventsOnAggregate);

            cache.AddOrUpdate(entity);

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
