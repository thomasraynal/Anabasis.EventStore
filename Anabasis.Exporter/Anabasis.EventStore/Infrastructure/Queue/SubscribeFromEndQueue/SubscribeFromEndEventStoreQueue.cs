using EventStore.ClientAPI;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Infrastructure.Queue.SubscribeFromEndQueue
{
  public class SubscribeFromEndEventStoreQueue : BaseEventStoreQueue
  {
    private readonly SubscribeFromEndEventStoreQueueConfiguration  _volatileEventStoreQueueConfiguration;

    public SubscribeFromEndEventStoreQueue(
      IConnectionStatusMonitor connectionMonitor,
      SubscribeFromEndEventStoreQueueConfiguration  volatileEventStoreQueueConfiguration,
      IEventTypeProvider eventTypeProvider,
      ILogger logger = null)
      : base(connectionMonitor, volatileEventStoreQueueConfiguration, eventTypeProvider, logger)
    {
      _volatileEventStoreQueueConfiguration = volatileEventStoreQueueConfiguration;

      InitializeAndRun();
    }

    protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {

      return Observable.Create<ResolvedEvent>(observer =>
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
        }

        var eventTypeFilter = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

        var filter = Filter.EventType.Prefix(eventTypeFilter);

        var subscription = connection.FilteredSubscribeToAllFrom(
          Position.End,
          filter,
          _volatileEventStoreQueueConfiguration.CatchUpSubscriptionFilteredSettings,
          eventAppeared: onEvent,
          liveProcessingStarted: onCaughtUp,
          subscriptionDropped: onSubscriptionDropped,
          userCredentials: _volatileEventStoreQueueConfiguration.UserCredentials);

        return Disposable.Create(() =>
        {
          subscription.Stop();

        });

      });
    }
  }
}
