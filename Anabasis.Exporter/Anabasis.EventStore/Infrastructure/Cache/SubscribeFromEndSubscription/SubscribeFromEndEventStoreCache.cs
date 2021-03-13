using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Infrastructure.Cache.VolatileSubscription
{
  public class SubscribeFromEndEventStoreCache<TKey, TAggregate> : BaseEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {

    private readonly SubscribeFromEndCacheConfiguration<TKey, TAggregate> _volatileEventStoreCacheConfiguration;

    public SubscribeFromEndEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      SubscribeFromEndCacheConfiguration<TKey, TAggregate> volatileEventStoreCacheConfiguration,
      IEventTypeProvider eventTypeProvider,
      ILogger logger = null) : base(connectionMonitor, volatileEventStoreCacheConfiguration, eventTypeProvider, logger)
    {
      _volatileEventStoreCacheConfiguration = volatileEventStoreCacheConfiguration;

      InitializeAndRun();
    }

    protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {

      return Observable.Create<ResolvedEvent>( observer =>
     {

       Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent resolvedEvent)
       {
         observer.OnNext(resolvedEvent);

         return Task.CompletedTask;
       }

       void onSubscriptionDropped(EventStoreCatchUpSubscription _, SubscriptionDropReason subscriptionDropReason, Exception exception)
       {
         switch (subscriptionDropReason)
         {
           case SubscriptionDropReason.UserInitiated:
           case SubscriptionDropReason.ConnectionClosed:
             break;

           case SubscriptionDropReason.NotAuthenticated:
           case SubscriptionDropReason.AccessDenied:
           case SubscriptionDropReason.SubscribingError:
           case SubscriptionDropReason.ServerError:
           case SubscriptionDropReason.CatchUpError:
           case SubscriptionDropReason.ProcessingQueueOverflow:
           case SubscriptionDropReason.EventHandlerException:
           case SubscriptionDropReason.MaxSubscribersReached:
           case SubscriptionDropReason.PersistentSubscriptionDeleted:
           case SubscriptionDropReason.Unknown:
           case SubscriptionDropReason.NotFound:

             throw new InvalidOperationException($"{nameof(SubscriptionDropReason)} {subscriptionDropReason} throwed the consumer in a invalid state");

           default:

             throw new InvalidOperationException($"{nameof(SubscriptionDropReason)} {subscriptionDropReason} not found");
         }
       }

       void onCaughtUp(EventStoreCatchUpSubscription _)
       {
         if (IsCaughtUp) return;

         Cache.Edit((cache) =>
         {
           cache.Clear();
         });

         IsCaughtUpSubject.OnNext(true);

       }

       var eventTypeFilter = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

       var filter = Filter.EventType.Prefix(eventTypeFilter);

       var subscription = connection.FilteredSubscribeToAllFrom(
         Position.End,
         filter,
         _volatileEventStoreCacheConfiguration.CatchUpSubscriptionFilteredSettings,
         eventAppeared: onEvent,
         liveProcessingStarted: onCaughtUp,
         subscriptionDropped: onSubscriptionDropped,
         userCredentials: _eventStoreCacheConfiguration.UserCredentials);

       return Disposable.Create(() =>
       {
         subscription.Stop();
       });

     });
    }
  }
}
