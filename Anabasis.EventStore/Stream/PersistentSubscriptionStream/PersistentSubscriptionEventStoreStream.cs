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
    public class PersistentSubscriptionEventStoreStream : BaseEventStoreStream
    {
        private readonly PersistentSubscriptionEventStoreStreamConfiguration _persistentEventStoreStreamConfiguration;
        private EventStorePersistentSubscriptionBase _eventStorePersistentSubscription;

        public PersistentSubscriptionEventStoreStream(IConnectionStatusMonitor connectionMonitor,
          PersistentSubscriptionEventStoreStreamConfiguration persistentEventStoreStreamConfiguration,
          IEventTypeProvider eventTypeProvider,
          ILoggerFactory loggerFactory) : base(connectionMonitor,
              persistentEventStoreStreamConfiguration,
              eventTypeProvider,
              loggerFactory.CreateLogger<PersistentSubscriptionEventStoreStream>())
        {
            _persistentEventStoreStreamConfiguration = persistentEventStoreStreamConfiguration;
        }

        public void Acknowledge(ResolvedEvent resolvedEvent)
        {
            Logger?.LogDebug($"{Id} => ACK event - {resolvedEvent.Event.EventId}");

            _eventStorePersistentSubscription.Acknowledge(resolvedEvent);
        }

        public override void Disconnect()
        {
            _eventStorePersistentSubscription.Stop(TimeSpan.FromSeconds(5));
            IsWiredUp = false;
        }

        public void NotAcknowledge(ResolvedEvent resolvedEvent, PersistentSubscriptionNakEventAction persistentSubscriptionNakEventAction, string reason = null)
        {
            Logger?.LogDebug($"{Id} => NACK event - {resolvedEvent.Event.EventId} - reason : {reason}");

            _eventStorePersistentSubscription.Fail(resolvedEvent, persistentSubscriptionNakEventAction, reason);
        }

        protected override IDisposable ConnectToEventStream(IEventStoreConnection connection)
        {

            void stopSubscription()
            {
                if (null != _eventStorePersistentSubscription)
                {
                    _eventStorePersistentSubscription.Stop(TimeSpan.FromSeconds(5));
                }
            }

            void onEvent(EventStorePersistentSubscriptionBase _, ResolvedEvent resolvedEvent)
            {
                Logger?.LogDebug($"{Id} => onEvent - {resolvedEvent.Event.EventId}");

                OnResolvedEvent(resolvedEvent);
            }

            void onSubscriptionDropped(EventStorePersistentSubscriptionBase _, SubscriptionDropReason subscriptionDropReason, Exception exception)
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
                            createNewPersistentSubscription().Wait();
                        }

                        break;
                }
            }

            async Task createNewPersistentSubscription()
            {
                stopSubscription();

                Logger?.LogInformation($"{Id} => ConnectToPersistentSubscriptionAsync - StreamId: {_persistentEventStoreStreamConfiguration.StreamId} - GroupId: {_persistentEventStoreStreamConfiguration.GroupId}");

                _eventStorePersistentSubscription = await connection.ConnectToPersistentSubscriptionAsync(
                 _persistentEventStoreStreamConfiguration.StreamId,
                 _persistentEventStoreStreamConfiguration.GroupId,
                 eventAppeared: onEvent,
                 subscriptionDropped: onSubscriptionDropped,
                 userCredentials: _persistentEventStoreStreamConfiguration.UserCredentials,
                 _persistentEventStoreStreamConfiguration.BufferSize,
                 _persistentEventStoreStreamConfiguration.AutoAck);
            };

            createNewPersistentSubscription().Wait();

            return Disposable.Create(() =>
              {
                  stopSubscription();
              });

        }


    }
}
