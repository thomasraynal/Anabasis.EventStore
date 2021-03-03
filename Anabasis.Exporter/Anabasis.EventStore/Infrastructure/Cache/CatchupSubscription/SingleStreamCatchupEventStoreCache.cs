using System;
using EventStore.ClientAPI;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription
{
  public class SingleStreamCatchupEventStoreCache<TKey, TAggregate> : BaseCatchupEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {

    private readonly SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate> _singleStreamCatchupEventStoreCacheConfiguration;

    public SingleStreamCatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration,
      IEventTypeProvider eventTypeProvider,
      ILogger logger = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, logger)
    {
      _singleStreamCatchupEventStoreCacheConfiguration = cacheConfiguration;

      InitializeAndRun();
    }

  
    protected override EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(IEventStoreConnection connection, Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent, Action<EventStoreCatchUpSubscription> onCaughtUp, Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped)
    {

      void onCaughtUpWithCheckpoint(EventStoreCatchUpSubscription _)
      {
        IsCaughtUpSubject.OnNext(true);
      }

      var subscription = connection.SubscribeToStreamFrom(
        _singleStreamCatchupEventStoreCacheConfiguration.StreamId,
       LastProcessedEventSequenceNumber,
        _singleStreamCatchupEventStoreCacheConfiguration.CatchUpSubscriptionSettings,
        onEvent,
        onCaughtUpWithCheckpoint,
        onSubscriptionDropped,
        userCredentials: _eventStoreCacheConfiguration.UserCredentials);

      return subscription;
    }
  }
}
