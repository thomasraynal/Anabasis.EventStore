using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Stream
{
    public abstract class BaseSubscribeToOneStreamEventStoreStream : BaseEventStoreStream
    {
        private readonly SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration _volatileSubscribeToOneStreamEventStoreStreamConfiguration;
        private readonly int _streamPosition;

        public BaseSubscribeToOneStreamEventStoreStream(
          int streamPosition,
          IConnectionStatusMonitor connectionMonitor,
          SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration subscribeToOneStreamEventStoreStreamConfiguration,
          IEventTypeProvider eventTypeProvider,
          Microsoft.Extensions.Logging.ILogger logger = null)
          : base(connectionMonitor, subscribeToOneStreamEventStoreStreamConfiguration, eventTypeProvider, logger)
        {
            _volatileSubscribeToOneStreamEventStoreStreamConfiguration = subscribeToOneStreamEventStoreStreamConfiguration;
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

                Logger?.LogInformation($"{Id} => ConnectToEventStream - SubscribeToStreamFrom - StreamId: {_volatileSubscribeToOneStreamEventStoreStreamConfiguration.StreamId} - StreamPosition: {_streamPosition}");

                var subscription = connection.SubscribeToStreamFrom(
                  _volatileSubscribeToOneStreamEventStoreStreamConfiguration.StreamId,
                  _streamPosition,
                  _volatileSubscribeToOneStreamEventStoreStreamConfiguration.CatchUpSubscriptionFilteredSettings,
                  eventAppeared: onEvent,
                  liveProcessingStarted: onCaughtUp,
                  subscriptionDropped: onSubscriptionDropped,
                  userCredentials: _volatileSubscribeToOneStreamEventStoreStreamConfiguration.UserCredentials);

                return Disposable.Create(() =>
                  {
                      subscription.Stop();
                  });

            });
        }
    }
}
