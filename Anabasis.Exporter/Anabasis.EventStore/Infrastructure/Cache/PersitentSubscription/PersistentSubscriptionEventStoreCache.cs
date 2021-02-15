using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using EventStore.ClientAPI;
using System.Linq;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Anabasis.EventStore.Infrastructure.Cache;

namespace Anabasis.EventStore.Infrastructure
{
  public class PersistentSubscriptionEventStoreCache<TKey, TCacheItem> : BaseEventStoreCache<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {
    private readonly PersistentSubscriptionCacheConfiguration<TKey, TCacheItem> _persistentSubscriptionCacheConfiguration;

    public PersistentSubscriptionEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      PersistentSubscriptionCacheConfiguration<TKey, TCacheItem> persistentSubscriptionCacheConfiguration,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      ILogger logger = null) : base(connectionMonitor, persistentSubscriptionCacheConfiguration, eventTypeProvider, logger)
    {
      _persistentSubscriptionCacheConfiguration = persistentSubscriptionCacheConfiguration;
    }

    protected override void OnInitialize(IEventStoreConnection connection)
    {

      IsStaleSubject.OnNext(true);

      IsCaughtUpSubject.OnNext(true);

      _eventsConnection.Disposable = ConnectToEventStream(connection)
                                      .Where(ev => CanApply(ev.EventType))
                                      .Subscribe(evt =>
                                      {
                                        UpdateCacheState(evt);
                                      });
    }

    private IObservable<RecordedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {

      return Observable.Create<RecordedEvent>(async obs =>
      {

        Task onEvent(EventStorePersistentSubscriptionBase _, ResolvedEvent e)
        {

          obs.OnNext(e.Event);

          return Task.FromResult<int?>(null);

        }

        void onSubscriptionDropped(EventStorePersistentSubscriptionBase _, SubscriptionDropReason subscriptionDropReason, Exception exception)
        {

        }

        await connection.CreatePersistentSubscriptionAsync(
          _persistentSubscriptionCacheConfiguration.StreamId,
          _persistentSubscriptionCacheConfiguration.GroupId,
          _persistentSubscriptionCacheConfiguration.PersistentSubscriptionSettings,
          _persistentSubscriptionCacheConfiguration.UserCredentials
      );

        var subscription = await connection.ConnectToPersistentSubscriptionAsync(
          _persistentSubscriptionCacheConfiguration.StreamId,
          _persistentSubscriptionCacheConfiguration.GroupId,
          onEvent,
          onSubscriptionDropped,
          _persistentSubscriptionCacheConfiguration.UserCredentials,
          autoAck: true);

        return Disposable.Create(() =>
        {
          subscription.Stop(TimeSpan.FromSeconds(10));
        });

      });
    }
  }
}
