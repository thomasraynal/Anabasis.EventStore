using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Queue
{
    public abstract class BaseSubscribeToOneStreamEventStoreQueue : BaseEventStoreQueue
    {
        private readonly SubscribeToOneStreamEventStoreQueueConfiguration _volatileSubscribeToOneStreamEventStoreQueueConfiguration;
        private readonly int _streamPosition;

        public BaseSubscribeToOneStreamEventStoreQueue(
          int streamPosition,
          IConnectionStatusMonitor connectionMonitor,
          SubscribeToOneStreamEventStoreQueueConfiguration subscribeToOneStreamEventStoreQueueConfiguration,
          IEventTypeProvider eventTypeProvider,
          Microsoft.Extensions.Logging.ILogger logger = null)
          : base(connectionMonitor, subscribeToOneStreamEventStoreQueueConfiguration, eventTypeProvider, logger)
        {
            _volatileSubscribeToOneStreamEventStoreQueueConfiguration = subscribeToOneStreamEventStoreQueueConfiguration;
            _streamPosition = streamPosition;
        }

        protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
        {

            return Observable.Create<ResolvedEvent>(observer =>
            {

                Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent resolvedEvent)
                {
                    Logger?.LogDebug($"{Id} => onEvent - {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} {resolvedEvent.Event.EventNumber}");

                    observer.OnNext(resolvedEvent);

                    return Task.CompletedTask;
                }

                void onSubscriptionDropped(EventStoreCatchUpSubscription _, SubscriptionDropReason subscriptionDropReason, Exception exception)
                {
                    switch (subscriptionDropReason)
                    {
                        case SubscriptionDropReason.UserInitiated:
                        case SubscriptionDropReason.ConnectionClosed:
                            Logger?.LogDebug(exception,$"{Id} => SubscriptionDropReason - reason : {subscriptionDropReason}");
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

                            throw new InvalidOperationException($"{nameof(SubscriptionDropReason)} {subscriptionDropReason} throwed the consumer in a invalid state", exception);

                        default:

                            throw new InvalidOperationException($"{nameof(SubscriptionDropReason)} {subscriptionDropReason} not found", exception);
                    }
                }

                void onCaughtUp(EventStoreCatchUpSubscription _)
                {
                }

                Logger?.LogInformation($"{Id} => ConnectToEventStream - SubscribeToStreamFrom - StreamId: {_volatileSubscribeToOneStreamEventStoreQueueConfiguration.StreamId} - StreamPosition: {_streamPosition}");

                var subscription = connection.SubscribeToStreamFrom(
                  _volatileSubscribeToOneStreamEventStoreQueueConfiguration.StreamId,
                  _streamPosition,
                  _volatileSubscribeToOneStreamEventStoreQueueConfiguration.CatchUpSubscriptionFilteredSettings,
                  eventAppeared: onEvent,
                  liveProcessingStarted: onCaughtUp,
                  subscriptionDropped: onSubscriptionDropped,
                  userCredentials: _volatileSubscribeToOneStreamEventStoreQueueConfiguration.UserCredentials);

                return Disposable.Create(() =>
                  {
                      subscription.Stop();
                  });

            });
        }
    }
}
