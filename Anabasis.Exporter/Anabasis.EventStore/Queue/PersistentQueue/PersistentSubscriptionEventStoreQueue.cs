using EventStore.ClientAPI;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Infrastructure.Queue.PersistentQueue
{
  public class PersistentSubscriptionEventStoreQueue : BaseEventStoreQueue
  {
    private readonly PersistentSubscriptionEventStoreQueueConfiguration _persistentEventStoreQueueConfiguration;

    public PersistentSubscriptionEventStoreQueue(IConnectionStatusMonitor connectionMonitor,
      PersistentSubscriptionEventStoreQueueConfiguration persistentEventStoreQueueConfiguration,
      IEventTypeProvider eventTypeProvider,
      Microsoft.Extensions.Logging.ILogger logger = null) : base(connectionMonitor, persistentEventStoreQueueConfiguration, eventTypeProvider, logger)
    {
      _persistentEventStoreQueueConfiguration = persistentEventStoreQueueConfiguration;

      InitializeAndRun();

    }

    protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {
      return Observable.Create<ResolvedEvent>(async observer =>
      {

        Task onEvent(EventStorePersistentSubscriptionBase _, ResolvedEvent resolvedEvent)
        {
          observer.OnNext(resolvedEvent);

          return Task.CompletedTask;
        }

        void onSubscriptionDropped(EventStorePersistentSubscriptionBase _, SubscriptionDropReason subscriptionDropReason, Exception exception)
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

        

        var subscription = await connection.ConnectToPersistentSubscriptionAsync(
         _persistentEventStoreQueueConfiguration.StreamId,
         _persistentEventStoreQueueConfiguration.GroupId,
         eventAppeared: onEvent,
         subscriptionDropped: onSubscriptionDropped,
         userCredentials: _persistentEventStoreQueueConfiguration.UserCredentials,
         _persistentEventStoreQueueConfiguration.BufferSize,
         _persistentEventStoreQueueConfiguration.AutoAck);

        return Disposable.Create(() =>
        {

          subscription.Stop(TimeSpan.FromSeconds(5));

        });

      });
    }
  }
}
