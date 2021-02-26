using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using EventStore.ClientAPI;
using DynamicData;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Linq;

namespace Anabasis.EventStore.Infrastructure.Cache
{
  public abstract class BaseEventStoreCache<TKey, TCacheItem> : IDisposable, IEventStoreCache<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {
    protected readonly IEventStoreCacheConfiguration<TKey, TCacheItem> _eventStoreCacheConfiguration;
    protected readonly IEventTypeProvider<TKey, TCacheItem> _eventTypeProvider;
    private readonly IConnectionStatusMonitor _connectionMonitor;
    private readonly ILogger _logger;

    private IDisposable _eventStoreConnectionStatus;
    private IDisposable _eventStreamConnectionDisposable;
    private readonly IDisposable _isStaleDisposable;

    private DateTime _lastProcessedEventTimestamp;

    protected SourceCache<TCacheItem, TKey> Cache { get; } = new SourceCache<TCacheItem, TKey>(item => item.EntityId);

    protected BehaviorSubject<bool> _connectionStatusSubject { get; }
    protected BehaviorSubject<bool> IsStaleSubject { get; }



    protected BehaviorSubject<bool> IsCaughtUpSubject { get; }

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

    public bool IsConnected => _connectionMonitor.IsConnected;

    public IObservable<bool> OnConnected => _connectionMonitor.OnConnected;

    public TCacheItem GetCurrent(TKey key)
    {
      return Cache.Items.FirstOrDefault(item => item.EntityId.Equals(key));
    }

    public TCacheItem[] GetCurrents()
    {
      return Cache.Items.ToArray();
    }

    public BaseEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      IEventStoreCacheConfiguration<TKey, TCacheItem> cacheConfiguration,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      ILogger logger = null)
    {

      _logger = logger ?? new DummyLogger();

      _eventStoreCacheConfiguration = cacheConfiguration;
      _eventTypeProvider = eventTypeProvider;
      _connectionMonitor = connectionMonitor;

      _lastProcessedEventTimestamp = DateTime.MinValue;

      _connectionStatusSubject = new BehaviorSubject<bool>(false);

      IsCaughtUpSubject = new BehaviorSubject<bool>(false);
      IsStaleSubject = new BehaviorSubject<bool>(true);

       _isStaleDisposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe(_ =>
      {
        if (DateTime.UtcNow > _lastProcessedEventTimestamp.Add(_eventStoreCacheConfiguration.IsStaleTimeSpan))
        {
          IsStaleSubject.OnNext(true);
        }

      });

    }

    protected void Run()
    {
      _eventStoreConnectionStatus = _connectionMonitor.GetEvenStoreConnectionStatus().Subscribe(connectionChanged =>
      {
        _connectionStatusSubject.OnNext(connectionChanged.IsConnected);

        if (connectionChanged.IsConnected)
        {

          OnInitialize(connectionChanged.IsConnected);

          _eventStreamConnectionDisposable = ConnectToEventStream(connectionChanged.Value)
              .Subscribe(@event =>
              {
                OnResolvedEvent(@event);

                if (IsStale) IsStaleSubject.OnNext(false);

                _lastProcessedEventTimestamp = DateTime.UtcNow;

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
    protected abstract void OnResolvedEvent(ResolvedEvent @event);

    public IObservableCache<TCacheItem, TKey> AsObservableCache()
    {
      return Cache.AsObservableCache();
    }

    public void Dispose()
    {

      OnDispose();

      _isStaleDisposable.Dispose();
      _eventStoreConnectionStatus.Dispose();
      _connectionStatusSubject.Dispose();

      IsCaughtUpSubject.Dispose();
      IsStaleSubject.Dispose();

      Cache.Dispose();
    }

    protected virtual void OnInitialize(bool isConnected) { }

    protected void UpdateCacheState(ResolvedEvent resolvedEvent, SourceCache<TCacheItem, TKey> specificCache = null)
    {
      var recordedEvent = resolvedEvent.Event;

      var cache = specificCache ?? Cache;

      var @eventType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

      var @event = recordedEvent.GetMutator<TKey, TCacheItem>(@eventType, _eventStoreCacheConfiguration.Serializer);

      if (null == @event)
      {
        throw new EventNotSupportedException(recordedEvent);
      }

      var entry = cache.Lookup(@event.EntityId);

      TCacheItem entity;

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
        entity = new TCacheItem();
      }

      entity.ApplyEvent(@event, false, _eventStoreCacheConfiguration.KeepAppliedEventsOnAggregate);

      cache.AddOrUpdate(entity);

    }

    protected virtual void OnDispose() { }

  }
}
