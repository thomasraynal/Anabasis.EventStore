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

      Run();
    }

    protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {
      throw new NotImplementedException();
    }

    protected override void OnInitialize(bool isConnected)
    {

      IsStaleSubject.OnNext(true);

      IsCaughtUpSubject.OnNext(true);

      //_eventStreamConnectionDisposable.Disposable = ConnectToEventStream(connection)
      //                                .Where(@event => CanApply(@event.EventType))
      //                                .Subscribe(@event =>
      //                                {
      //                                  UpdateCacheState(@event);
      //                                });
    }

    protected override void OnResolvedEvent(ResolvedEvent @event)
    {
      throw new NotImplementedException();
    }

    //private IObservable<RecordedEvent> ConnectToEventStream(IEventStoreConnection connection)
    //{

    //  return Observable.Create<RecordedEvent>(async obs =>
    //  {

    //    Task onEvent(EventStorePersistentSubscriptionBase _, ResolvedEvent e)
    //    {

    //      obs.OnNext(e.Event);

    //      return Task.FromResult<int?>(null);

    //    }

    //    void onSubscriptionDropped(EventStorePersistentSubscriptionBase _, SubscriptionDropReason subscriptionDropReason, Exception exception)
    //    {

    //    }

    //    await connection.CreatePersistentSubscriptionAsync(
    //      _persistentSubscriptionCacheConfiguration.StreamId,
    //      _persistentSubscriptionCacheConfiguration.GroupId,
    //      _persistentSubscriptionCacheConfiguration.PersistentSubscriptionSettings,
    //      _persistentSubscriptionCacheConfiguration.UserCredentials
    //  );

    //    var subscription = await connection.ConnectToPersistentSubscriptionAsync(
    //      _persistentSubscriptionCacheConfiguration.StreamId,
    //      _persistentSubscriptionCacheConfiguration.GroupId,
    //      onEvent,
    //      onSubscriptionDropped,
    //      _persistentSubscriptionCacheConfiguration.UserCredentials,
    //      autoAck: true);

    //    return Disposable.Create(() =>
    //    {
    //      subscription.Stop(TimeSpan.FromSeconds(10));
    //    });

    //  });
    //}
  }
}
