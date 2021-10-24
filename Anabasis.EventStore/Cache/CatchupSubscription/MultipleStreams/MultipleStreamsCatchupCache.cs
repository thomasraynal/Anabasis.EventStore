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
    //harmonize with Cache base classes
    public class MultipleStreamsCatchupCache<TKey, TAggregate> : IEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        private readonly MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> _multipleStreamsCatchupCacheConfiguration;
    
        private readonly ISnapshotStrategy<TKey> _snapshotStrategy;
        private readonly ISnapshotStore<TKey, TAggregate> _snapshotStore;
        private readonly IConnectionStatusMonitor _connectionMonitor;
        private readonly DateTime _lastProcessedEventUtcTimestamp;
        private readonly ILogger<MultipleStreamsCatchupCache<TKey, TAggregate>> _logger;

        private readonly CompositeDisposable _cleanUp;

        private readonly SourceCache<TAggregate, TKey> _cache;
        private readonly SourceCache<TAggregate, TKey> _caughtingUpCache;
        
        private readonly BehaviorSubject<bool> _connectionStatusSubject;
        private readonly BehaviorSubject<bool> _isCaughtUpSubject;
        private readonly BehaviorSubject<bool> _isStaleSubject;

        private IDisposable _eventStoreConnectionStatus;
        private readonly object _catchUpLocker = new();
        private readonly List<MultipleStreamsCatchupCacheSubscriptionHolder<TKey, TAggregate>> _multipleStreamsCatchupCacheSubscriptionHolders;

        public IEventTypeProvider<TKey, TAggregate> EventTypeProvider { get; }
        public string Id { get; }
        public bool IsWiredUp { get; private set; }
        public IObservable<bool> OnStale => _isStaleSubject.AsObservable();
        public IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
        public bool IsStale => _isStaleSubject.Value;
        public bool IsCaughtUp => _isCaughtUpSubject.Value;
        public bool IsConnected => _connectionMonitor.IsConnected && IsWiredUp;
        public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;

        public IMultipleStreamsCatchupCacheSubscriptionHolder[] GetSubscriptionStates()
        {
            return _multipleStreamsCatchupCacheSubscriptionHolders.ToArray();
        }

        public MultipleStreamsCatchupCache(IConnectionStatusMonitor connectionMonitor,
           MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> multipleStreamsCatchupCacheConfiguration,
           IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
           ILoggerFactory loggerFactory,
           ISnapshotStore<TKey, TAggregate> snapshotStore = null,
           ISnapshotStrategy<TKey> snapshotStrategy = null)
        {

            if(multipleStreamsCatchupCacheConfiguration.UseSnapshot && snapshotStore == null && snapshotStrategy == null)
            {
                throw new InvalidOperationException($"{nameof(MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate>)}.UseSnapshot " +
                    $"is set to true but no snapshotStore and/or snapshotStrategy are provided");
            }

            IsWiredUp = false;
            Id = $"{GetType()}-{Guid.NewGuid()}";
            EventTypeProvider = eventTypeProvider;

            _cache = new SourceCache<TAggregate, TKey>(item => item.EntityId);
            _caughtingUpCache = new SourceCache<TAggregate, TKey>(item => item.EntityId);
            _logger = loggerFactory?.CreateLogger<MultipleStreamsCatchupCache<TKey, TAggregate>>();
            _multipleStreamsCatchupCacheConfiguration = multipleStreamsCatchupCacheConfiguration;
            _connectionMonitor = connectionMonitor;
            _snapshotStrategy = snapshotStrategy;
            _snapshotStore = snapshotStore;
            _lastProcessedEventUtcTimestamp = DateTime.MinValue;
            _connectionStatusSubject = new BehaviorSubject<bool>(false);
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
            _isStaleSubject = new BehaviorSubject<bool>(true);
            _cleanUp = new CompositeDisposable();

            _multipleStreamsCatchupCacheSubscriptionHolders = multipleStreamsCatchupCacheConfiguration.StreamIds.Select(streamId =>
            {
                return new MultipleStreamsCatchupCacheSubscriptionHolder<TKey,TAggregate>()
                {
                    StreamId = streamId
                };

            }).ToList();

            foreach(var multipleStreamsCatchupCacheSubscriptionHolder in _multipleStreamsCatchupCacheSubscriptionHolders)
            {
                var subscription = multipleStreamsCatchupCacheSubscriptionHolder.OnCaughtUp.Subscribe(hasStreamSubscriptionCaughtUp =>
                {

                    lock (_catchUpLocker)
                    {

                        if (hasStreamSubscriptionCaughtUp && !IsCaughtUp)
                        {
                            if (_multipleStreamsCatchupCacheSubscriptionHolders.All(holder => holder.IsCaughtUp))
                            {

                                _logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");

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

            _cleanUp.Add(isStaleSubscription);
        }

        public IObservableCache<TAggregate, TKey> AsObservableCache()
        {
            return _cache.AsObservableCache();
        }

        private string[] GetEventsFilters()
        {
            var eventTypeFilters = EventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

            return eventTypeFilters;
        }

        private SourceCache<TAggregate, TKey> GetCurrentCache()
        {
            return IsCaughtUp ? _cache : _caughtingUpCache;
        }

        protected async Task OnLoadSnapshot()
        {
            if (_multipleStreamsCatchupCacheConfiguration.UseSnapshot)
            {
                var eventTypeFilter = GetEventsFilters();

                foreach (var multipleStreamsCatchupCacheSubscriptionHolder in _multipleStreamsCatchupCacheSubscriptionHolders)
                {
                    var snapshot = await _snapshotStore.GetByVersionOrLast(multipleStreamsCatchupCacheSubscriptionHolder.StreamId, eventTypeFilter);

                    if (null == snapshot) continue;

                    multipleStreamsCatchupCacheSubscriptionHolder.CurrentSnapshotEventVersion = snapshot.VersionFromSnapshot;
                    multipleStreamsCatchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = snapshot.VersionFromSnapshot;
                    multipleStreamsCatchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

                    _logger?.LogInformation($"{Id} => OnLoadSnapshot - EntityId: {snapshot.EntityId} StreamId: {snapshot.StreamId}");

                    var cache = GetCurrentCache();

                    cache.AddOrUpdate(snapshot);

                }
            }
        }
        

        protected void OnResolvedEvent(ResolvedEvent @event)
        {
            var cache = GetCurrentCache();

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

            _cleanUp.Add(_eventStoreConnectionStatus);
        }

        public TAggregate GetCurrent(TKey key)
        {
            return _cache.Items.FirstOrDefault(item => item.EntityId.Equals(key));
        }

        public TAggregate[] GetCurrents()
        {
            return _cache.Items.ToArray();
        }

        private IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection,
            MultipleStreamsCatchupCacheSubscriptionHolder<TKey, TAggregate> multipleStreamsCatchupCacheSubscriptionHolder)
        {

            return Observable.Create<ResolvedEvent>(obs =>
            {

                 Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent @event)
                {
                    lock (_catchUpLocker)
                    {

                        _logger?.LogDebug($"{Id} => OnEvent {@event.Event.EventType} - v.{@event.Event.EventNumber}");

                        obs.OnNext(@event);

                        if (IsCaughtUp && _multipleStreamsCatchupCacheConfiguration.UseSnapshot)
                        {
                            foreach (var aggregate in _cache.Items)
                            {
                                if (_snapshotStrategy.IsSnapShotRequired(aggregate))
                                {
                                    _logger?.LogInformation($"{Id} => Snapshoting aggregate => {aggregate.EntityId} {aggregate.GetType()} - v.{aggregate.Version}");

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

                var subscription = GetEventStoreCatchUpSubscription(multipleStreamsCatchupCacheSubscriptionHolder,
                    connection, onEvent, onSubscriptionDropped);

                return Disposable.Create(() =>
                {
                    subscription.Stop();
                });

            });
        }

        private EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(
            MultipleStreamsCatchupCacheSubscriptionHolder<TKey, TAggregate> multipleStreamsCatchupCacheSubscriptionHolder,
            IEventStoreConnection connection,
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent,
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped)
        {

            void onCaughtUp(EventStoreCatchUpSubscription _)
            {
                multipleStreamsCatchupCacheSubscriptionHolder.OnCaughtUpSubject.OnNext(true);
            }

            long? subscribeFromPosition = multipleStreamsCatchupCacheSubscriptionHolder.CurrentSnapshotEventVersion == null ?
                null : multipleStreamsCatchupCacheSubscriptionHolder.CurrentSnapshotEventVersion;

            _logger?.LogInformation($"{Id} => GetEventStoreCatchUpSubscription - SubscribeToStreamFrom {multipleStreamsCatchupCacheSubscriptionHolder.StreamId} " +
                $"v.{subscribeFromPosition}]");

            var subscription = connection.SubscribeToStreamFrom(
              multipleStreamsCatchupCacheSubscriptionHolder.StreamId,
              subscribeFromPosition,
              _multipleStreamsCatchupCacheConfiguration.CatchUpSubscriptionFilteredSettings,
              onEvent,
              onCaughtUp,
              onSubscriptionDropped,
              userCredentials: _multipleStreamsCatchupCacheConfiguration.UserCredentials);

            return subscription;
        }

        private IMutation<TKey, TAggregate> DeserializeEvent(RecordedEvent recordedEvent)
        {
            var targetType = EventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

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
            _cleanUp.Dispose();
        }
    }
}
