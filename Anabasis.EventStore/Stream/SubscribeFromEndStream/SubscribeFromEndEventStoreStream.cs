using Anabasis.Common;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeFromEndEventStoreStream : BaseEventStoreStream
    {
        private readonly SubscribeFromEndEventStoreStreamConfiguration _volatileEventStoreStreamConfiguration;
        private EventStoreAllFilteredCatchUpSubscription _eventStoreAllFilteredCatchUpSubscription;

        public SubscribeFromEndEventStoreStream(
          IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
          SubscribeFromEndEventStoreStreamConfiguration volatileEventStoreStreamConfiguration,
          IEventTypeProvider eventTypeProvider,
          ILoggerFactory loggerFactory)
          : base(connectionMonitor, volatileEventStoreStreamConfiguration, eventTypeProvider, loggerFactory.CreateLogger<SubscribeFromEndEventStoreStream>())
        {
            _volatileEventStoreStreamConfiguration = volatileEventStoreStreamConfiguration;
        }

        public override void Disconnect()
        {
            _eventStoreAllFilteredCatchUpSubscription.Stop();
        }

        protected override IDisposable ConnectToEventStream(IEventStoreConnection connection)
        {

            void stopSubscription()
            {
                if (null != _eventStoreAllFilteredCatchUpSubscription)
                {
                    _eventStoreAllFilteredCatchUpSubscription.Stop();
                }
            }

            Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent resolvedEvent)
            {
                Logger?.LogDebug($"{Id} => onEvent - {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} {resolvedEvent.Event.EventNumber}");

                OnResolvedEvent(resolvedEvent);

                return Task.CompletedTask;
            }

            void onSubscriptionDropped(EventStoreCatchUpSubscription _, SubscriptionDropReason subscriptionDropReason, Exception exception)
            {
                switch (subscriptionDropReason)
                {
                    case SubscriptionDropReason.UserInitiated:
                        break;
                    case SubscriptionDropReason.ConnectionClosed:
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
                    default:

                        Logger?.LogError(exception, $"{nameof(SubscriptionDropReason)}: [{subscriptionDropReason}] throwed the consumer in an invalid state");

                        if (_eventStoreStreamConfiguration.DoAppCrashIfSubscriptionFail)
                        {
                            KillSwitch.KillMe(exception);
                        }
                        else
                        {
                            createNewFilteredCatchupSubscription();
                        }

                        break;
                }
            }

            void onCaughtUp(EventStoreCatchUpSubscription _)
            {
            }

            void createNewFilteredCatchupSubscription()
            {
                stopSubscription();

                var eventTypeFilter = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

                var filter = Filter.EventType.Prefix(eventTypeFilter);

                var position = Position.End;

                Logger?.LogInformation($"{Id} => ConnectToEventStream - FilteredSubscribeToAllFrom - Position: {position} Filters: [{string.Join("|", eventTypeFilter)}]");

                _eventStoreAllFilteredCatchUpSubscription = connection.FilteredSubscribeToAllFrom(
                    position,
                    filter,
                    _volatileEventStoreStreamConfiguration.CatchUpSubscriptionFilteredSettings,
                    eventAppeared: onEvent,
                    liveProcessingStarted: onCaughtUp,
                    subscriptionDropped: onSubscriptionDropped,
                    userCredentials: _volatileEventStoreStreamConfiguration.UserCredentials);

            }

            createNewFilteredCatchupSubscription();

            return Disposable.Create(() =>
            {
                stopSubscription();
            });

        }

        protected override IMessage CreateMessage(IEvent @event, ResolvedEvent resolvedEvent)
        {
            return new EventStoreCatchupSubscriptionMessage(@event.EventId, @event, resolvedEvent);
        }
    }
}
