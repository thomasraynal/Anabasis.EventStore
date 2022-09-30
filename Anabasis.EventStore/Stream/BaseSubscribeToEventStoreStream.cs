using Anabasis.Common;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore2.Configuration;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Disposables;

namespace Anabasis.EventStore.Stream
{
    public abstract class BaseSubscribeToEventStoreStream : BaseEventStoreStream
    {
        private readonly SubscribeToOneStreamConfiguration _subscribeToOneStreamConfiguration;
        private EventStoreStreamCatchUpSubscription? _eventStoreCatchupSubscription;
        private readonly int _streamPosition;
        private readonly IKillSwitch _killSwitch;
        
        public BaseSubscribeToEventStoreStream(
          int streamPosition,
          IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
          SubscribeToOneStreamConfiguration subscribeToOneStreamConfiguration,
          IEventTypeProvider eventTypeProvider,
          ILoggerFactory? loggerFactory = null,
          IKillSwitch? killSwitch = null)
          : base(connectionMonitor, subscribeToOneStreamConfiguration, eventTypeProvider, loggerFactory)
        {
            _subscribeToOneStreamConfiguration = subscribeToOneStreamConfiguration;
            _streamPosition = streamPosition;
            _killSwitch = killSwitch ?? new KillSwitch();
        }

        public override void Disconnect()
        {
            _eventStoreCatchupSubscription?.Stop();
        }

        protected override IDisposable ConnectToEventStream(IEventStoreConnection connection)
        {

            void stopSubscription()
            {
                if (null != _eventStoreCatchupSubscription)
                {
                    _eventStoreCatchupSubscription.Stop();
                }
            }

            void onEvent(EventStoreCatchUpSubscription _, ResolvedEvent resolvedEvent)
            {
                Logger?.LogDebug($"{Id} => onEvent - {resolvedEvent.Event.EventId} {resolvedEvent.Event.EventStreamId} {resolvedEvent.Event.EventNumber}");

                OnResolvedEvent(resolvedEvent);
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

                        if (_eventStoreStreamConfiguration.DoAppCrashOnFailure)
                        {
                            _killSwitch.KillProcess(exception);
                        }
                        else
                        {
                            createNewCatchupSubscription();
                        }

                        break;
                }
            }

            void onCaughtUp(EventStoreCatchUpSubscription _)
            {
            }

            void createNewCatchupSubscription()
            {
                stopSubscription();

                Logger?.LogInformation($"{Id} => SubscribeToStreamFrom - StreamId: {_subscribeToOneStreamConfiguration.StreamId} - StreamPosition: {_streamPosition}");

                _eventStoreCatchupSubscription = connection.SubscribeToStreamFrom(
                  _subscribeToOneStreamConfiguration.StreamId,
                  _streamPosition,
                  _subscribeToOneStreamConfiguration.CatchUpSubscriptionFilteredSettings,
                  eventAppeared: onEvent,
                  liveProcessingStarted: onCaughtUp,
                  subscriptionDropped: onSubscriptionDropped,
                  userCredentials: _subscribeToOneStreamConfiguration.UserCredentials);

            }

            createNewCatchupSubscription();

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
