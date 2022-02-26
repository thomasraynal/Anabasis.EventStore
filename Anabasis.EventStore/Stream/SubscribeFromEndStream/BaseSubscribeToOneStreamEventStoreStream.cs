using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Stream
{
    public abstract class BaseSubscribeToOneStreamEventStoreStream : BaseEventStoreStream
    {
        private readonly SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration _volatileSubscribeToOneStreamEventStoreStreamConfiguration;
        private readonly int _streamPosition;
        private EventStoreStreamCatchUpSubscription _eventStoreCatchupSubscription;

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

        public override void Disconnect()
        {
            _eventStoreCatchupSubscription.Stop();
            IsWiredUp = false;
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

                        if (_eventStoreStreamConfiguration.DoAppCrashIfSubscriptionFail)
                        {
                            Scheduler.Default.Schedule(() => ExceptionDispatchInfo.Capture(exception).Throw());
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

                Logger?.LogInformation($"{Id} => SubscribeToStreamFrom - StreamId: {_volatileSubscribeToOneStreamEventStoreStreamConfiguration.StreamId} - StreamPosition: {_streamPosition}");

                _eventStoreCatchupSubscription = connection.SubscribeToStreamFrom(
                  _volatileSubscribeToOneStreamEventStoreStreamConfiguration.StreamId,
                  _streamPosition,
                  _volatileSubscribeToOneStreamEventStoreStreamConfiguration.CatchUpSubscriptionFilteredSettings,
                  eventAppeared: onEvent,
                  liveProcessingStarted: onCaughtUp,
                  subscriptionDropped: onSubscriptionDropped,
                  userCredentials: _volatileSubscribeToOneStreamEventStoreStreamConfiguration.UserCredentials);

            }


            createNewCatchupSubscription();

            return Disposable.Create(() =>
              {
                  stopSubscription();
              });


        }
    }
}
