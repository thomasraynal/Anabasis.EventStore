using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Queue
{
    public class SubscribeFromEndEventStoreQueue : BaseEventStoreQueue
    {
        private readonly SubscribeFromEndEventStoreQueueConfiguration _volatileEventStoreQueueConfiguration;
        private EventStoreAllFilteredCatchUpSubscription _subscription;

        public SubscribeFromEndEventStoreQueue(
          IConnectionStatusMonitor connectionMonitor,
          SubscribeFromEndEventStoreQueueConfiguration volatileEventStoreQueueConfiguration,
          IEventTypeProvider eventTypeProvider,
          ILoggerFactory loggerFactory)
          : base(connectionMonitor, volatileEventStoreQueueConfiguration, eventTypeProvider, loggerFactory.CreateLogger<SubscribeFromEndEventStoreQueue>())
        {
            _volatileEventStoreQueueConfiguration = volatileEventStoreQueueConfiguration;
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
                            Logger?.LogDebug(exception, $"{Id} => SubscriptionDropReason - reason : {subscriptionDropReason}");
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

                var eventTypeFilter = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

                var filter = Filter.EventType.Prefix(eventTypeFilter);

                var position = Position.End;

                Logger?.LogInformation($"{Id} => ConnectToEventStream - FilteredSubscribeToAllFrom - Position: {position} Filters: [{string.Join("|", eventTypeFilter)}]");

                _subscription = connection.FilteredSubscribeToAllFrom(
                    position,
                    filter,
                    _volatileEventStoreQueueConfiguration.CatchUpSubscriptionFilteredSettings,
                    eventAppeared: onEvent,
                    liveProcessingStarted: onCaughtUp,
                    subscriptionDropped: onSubscriptionDropped,
                    userCredentials: _volatileEventStoreQueueConfiguration.UserCredentials);

                return Disposable.Create(() =>
                {
                    _subscription.Stop();
                });

            });
        }
    }
}
