using DynamicData;
using EventStore.ClientAPI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription
{
  public abstract class BaseCatchupEventStoreCache<TKey, TAggregate> : BaseEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {
    protected SourceCache<TAggregate, TKey> CaughtingUpCache { get; } = new SourceCache<TAggregate, TKey>(item => item.EntityId);

    protected BaseCatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor, IEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration, IEventTypeProvider eventTypeProvider, ILogger logger = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, logger)
    {
    }

    protected abstract EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(IEventStoreConnection connection,
      Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent, Action<EventStoreCatchUpSubscription> onCaughtUp,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped);

    protected override void OnDispose()
    {
      CaughtingUpCache.Dispose();
    }

    protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {

      return Observable.Create<ResolvedEvent>(obs =>
      {

        Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent @event)
        {

          obs.OnNext(@event);

          return Task.CompletedTask;
        }

        void onCaughtUp(EventStoreCatchUpSubscription _)
        {

          //this handle a caughting up NOT due to disconnection, i.e a caughting up due to a lag
          if (IsCaughtUp) return;

          Cache.Edit(innerCache =>
          {

            innerCache.Load(CaughtingUpCache.Items);

            CaughtingUpCache.Clear();

          });

          IsCaughtUpSubject.OnNext(true);

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

        var subscription = GetEventStoreCatchUpSubscription(connection, onEvent, onCaughtUp, onSubscriptionDropped);

        return Disposable.Create(() =>
        {
          subscription.Stop();
        });

      });
    }

  }
}
