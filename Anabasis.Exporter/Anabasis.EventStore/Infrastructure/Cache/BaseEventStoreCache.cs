using System;
using System.Collections.Generic;
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
    private readonly IEventTypeProvider<TKey, TCacheItem> _eventTypeProvider;
    private readonly ILogger _logger;

    private CompositeDisposable _cleanup { get; }

    protected readonly SerialDisposable _eventStreamConnectionDisposable = new SerialDisposable();
    private readonly SerialDisposable _eventStoreConnectionDisposable = new SerialDisposable();

    protected SourceCache<TCacheItem, TKey> Cache { get; } = new SourceCache<TCacheItem, TKey>(item => item.EntityId);
    protected BehaviorSubject<bool> _connectionStatusSubject { get; }
    protected BehaviorSubject<bool> IsStaleSubject { get; }
    protected BehaviorSubject<bool> IsCaughtUpSubject { get; }

    public IObservable<bool> IsStale
    {
      get
      {
        return IsStaleSubject.AsObservable();
      }
    }

    public IObservable<bool> IsCaughtUp
    {
      get
      {
        return IsCaughtUpSubject.AsObservable();
      }
    }

    public TCacheItem GetCurrent(TKey key)
    {
      return Cache.Items.FirstOrDefault(item => item.EntityId.Equals(key));
    }

    public BaseEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      IEventStoreCacheConfiguration<TKey, TCacheItem> cacheConfiguration,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      ILogger logger = null)
    {
  
      _logger = logger ?? new DummyLogger();
      _eventStoreCacheConfiguration = cacheConfiguration;
      _eventTypeProvider = eventTypeProvider;

      _cleanup = new CompositeDisposable(_eventStreamConnectionDisposable, _eventStoreConnectionDisposable);

      _connectionStatusSubject = new BehaviorSubject<bool>(false);

      IsCaughtUpSubject = new BehaviorSubject<bool>(false);
      IsStaleSubject = new BehaviorSubject<bool>(true);

      _eventStoreConnectionDisposable.Disposable = connectionMonitor.GetEvenStoreConnectionStatus().Subscribe(connectionChanged =>
      {
        _connectionStatusSubject.OnNext(connectionChanged.IsConnected);

        if (connectionChanged.IsConnected)
        {
          OnInitialize(connectionChanged.Value);
        }
        else
        {
          if (!IsStaleSubject.Value)
          {
            IsStaleSubject.OnNext(true);
          }
        }
      });

    }

    public IObservableCache<TCacheItem, TKey> AsObservableCache()
    {
      return Cache.AsObservableCache();
    }

    public virtual void Dispose()
    {
      _cleanup.Dispose();
    }

    protected bool CanApply(string eventType)
    {
      return null != _eventTypeProvider.GetEventTypeByName(eventType);
    }

    protected abstract void OnInitialize(IEventStoreConnection connection);

    protected void UpdateCacheState(RecordedEvent recordedEvent, SourceCache<TCacheItem, TKey> specificCache = null)
    {

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

  }
}
