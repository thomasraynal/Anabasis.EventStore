using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using EventStore.ClientAPI;
using DynamicData;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Linq;
using Anabasis.EventStore.Snapshot;
using System.Threading.Tasks;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseEventStoreCache<TKey, TAggregate> : IEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        protected readonly IEventStoreCacheConfiguration<TKey, TAggregate> _eventStoreCacheConfiguration;
        
        protected readonly ISnapshotStrategy<TKey> _snapshotStrategy;
        protected readonly ISnapshotStore<TKey, TAggregate> _snapshotStore;

        private readonly IConnectionStatusMonitor _connectionMonitor;
        private IDisposable _eventStoreConnectionStatus;
        private IDisposable _eventStreamConnectionDisposable;
        private readonly IDisposable _isStaleDisposable;
        private DateTime _lastProcessedEventUtcTimestamp;

        public IEventTypeProvider<TKey, TAggregate> EventTypeProvider { get; }
        public bool IsWiredUp { get; private set; }
        protected ILogger Logger { get; private set; }
        protected SourceCache<TAggregate, TKey> Cache { get; }
        protected BehaviorSubject<bool> ConnectionStatusSubject { get; private set; }
        protected BehaviorSubject<bool> IsStaleSubject { get; private set; }
        protected BehaviorSubject<bool> IsCaughtUpSubject { get; private set; }
        protected long? LastProcessedEventSequenceNumber { get; set; } = null;
        public IObservable<bool> OnStale => IsStaleSubject.AsObservable();
        public IObservable<bool> OnCaughtUp => IsCaughtUpSubject.AsObservable();

        public bool IsStale
        {
            get
            {
                return IsStaleSubject.Value;
            }
        }

        public bool IsCaughtUp
        {
            get
            {
                return IsCaughtUpSubject.Value;
            }
        }

        public bool IsConnected => _connectionMonitor.IsConnected && IsWiredUp;

        public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;

        public string Id { get; private set; }

        public TAggregate GetCurrent(TKey key)
        {
            return Cache.Items.FirstOrDefault(item => item.EntityId.Equals(key));
        }

        public TAggregate[] GetCurrents()
        {
            return Cache.Items.ToArray();
        }

        public BaseEventStoreCache(IConnectionStatusMonitor connectionMonitor,
           IEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration,
           IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
           ILoggerFactory loggerFactory,
           ISnapshotStore<TKey, TAggregate> snapshotStore = null,
           ISnapshotStrategy<TKey> snapshotStrategy = null)
        {

            if (cacheConfiguration.UseSnapshot && snapshotStore == null && snapshotStrategy == null)
            {
                throw new InvalidOperationException($"{cacheConfiguration.GetType().Name}.UseSnapshot " +
                    $"is set to true but no snapshotStore and/or snapshotStrategy are provided");
            }

            Cache = new SourceCache<TAggregate, TKey>(item => item.EntityId);
            EventTypeProvider = eventTypeProvider;
            IsWiredUp = false;

            _eventStoreCacheConfiguration = cacheConfiguration;
            _connectionMonitor = connectionMonitor;
            _snapshotStrategy = snapshotStrategy;
            _snapshotStore = snapshotStore;
            _lastProcessedEventUtcTimestamp = DateTime.MinValue;

            Logger = loggerFactory?.CreateLogger(GetType());

            Id = $"{GetType()}-{Guid.NewGuid()}";

            ConnectionStatusSubject = new BehaviorSubject<bool>(false);

            IsCaughtUpSubject = new BehaviorSubject<bool>(false);
            IsStaleSubject = new BehaviorSubject<bool>(true);

            _isStaleDisposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
            {
                if (DateTime.UtcNow > _lastProcessedEventUtcTimestamp.Add(_eventStoreCacheConfiguration.IsStaleTimeSpan))
                {
                    IsStaleSubject.OnNext(true);
                }

            });
        }



        public void Connect()
        {
            if (IsWiredUp) return;

            Logger?.LogDebug($"{Id} => Connecting");

            IsWiredUp = true;

            _eventStoreConnectionStatus = _connectionMonitor.GetEvenStoreConnectionStatus().Subscribe(async connectionChanged =>
          {
              Logger?.LogDebug($"{Id} => IsConnected: {connectionChanged.IsConnected}");

              ConnectionStatusSubject.OnNext(connectionChanged.IsConnected);

              if (connectionChanged.IsConnected)
              {

                  await OnLoadSnapshot();

                  OnInitialize(connectionChanged.IsConnected);

                  _eventStreamConnectionDisposable = ConnectToEventStream(connectionChanged.Value)
                    .Subscribe(@event =>
                    {
                        OnResolvedEvent(@event);

                        if (IsStale) IsStaleSubject.OnNext(false);

                        LastProcessedEventSequenceNumber = @event.Event.EventNumber;
                        _lastProcessedEventUtcTimestamp = DateTime.UtcNow;

                    });

              }
              else
              {

                  if (null != _eventStreamConnectionDisposable) _eventStreamConnectionDisposable.Dispose();

                  if (IsCaughtUp)
                  {
                      IsCaughtUpSubject.OnNext(false);
                  }

              }
          });
        }

        protected abstract IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection);
        protected virtual void OnResolvedEvent(ResolvedEvent @event)
        {
            Logger?.LogDebug($"{Id} => OnResolvedEvent: {@event.Event.EventId} {@event.Event.EventStreamId} - v.{@event.Event.EventNumber}");

            UpdateCacheState(@event, Cache);
        }

        public IObservableCache<TAggregate, TKey> AsObservableCache()
        {
            return Cache.AsObservableCache();
        }

        public virtual void Dispose()
        {

            _isStaleDisposable.Dispose();
            _eventStoreConnectionStatus.Dispose();

            ConnectionStatusSubject.OnCompleted();
            IsCaughtUpSubject.OnCompleted();
            IsStaleSubject.OnCompleted();

            ConnectionStatusSubject.Dispose();
            IsCaughtUpSubject.Dispose();
            IsStaleSubject.Dispose();

            Cache.Dispose();
        }

        protected virtual Task OnLoadSnapshot()
        {
            return Task.CompletedTask;
        }

        protected virtual void OnInitialize(bool isConnected) { }

        private IMutation<TKey, TAggregate> DeserializeEvent(RecordedEvent recordedEvent)
        {
            var targetType = EventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            if (null == targetType) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");

            return _eventStoreCacheConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IMutation<TKey, TAggregate>;
        }

        protected void UpdateCacheState(ResolvedEvent resolvedEvent, SourceCache<TAggregate, TKey> specificCache = null)
        {
            var recordedEvent = resolvedEvent.Event;

            Logger?.LogDebug($"{Id} => UpdateCacheState: {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} - v.{resolvedEvent.Event.EventNumber}");

            var cache = specificCache ?? Cache;

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
            }

            Logger?.LogDebug($"{Id} => Updating aggregate: {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} - v.{resolvedEvent.Event.EventNumber}");

            entity.ApplyEvent(@event, false, _eventStoreCacheConfiguration.KeepAppliedEventsOnAggregate);

            cache.AddOrUpdate(entity);

        }

    }
}
