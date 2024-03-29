using Anabasis.Common;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Disposables;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Stream
{
    public class PersistentSubscriptionEventStoreStream : BaseEventStoreStream
    {
        private readonly PersistentSubscriptionStreamConfiguration _persistentEventStoreStreamConfiguration;
        private readonly IKillSwitch _killSwitch;
        private EventStorePersistentSubscriptionBase? _eventStorePersistentSubscription;

        public PersistentSubscriptionEventStoreStream(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
          PersistentSubscriptionStreamConfiguration persistentEventStoreStreamConfiguration,
          IEventTypeProvider eventTypeProvider,
          ILoggerFactory? loggerFactory= null,
          IKillSwitch? killSwitch = null) : base(connectionMonitor,
              persistentEventStoreStreamConfiguration,
              eventTypeProvider,
              loggerFactory)
        {
            _persistentEventStoreStreamConfiguration = persistentEventStoreStreamConfiguration;
            _killSwitch = killSwitch?? new KillSwitch();
        }

        public override void Disconnect()
        {
            _eventStorePersistentSubscription?.Stop(TimeSpan.FromSeconds(5));
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

                        if (_eventStoreStreamConfiguration.DoAppCrashOnFailure)
                        {
                            _killSwitch.KillProcess(exception);
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

                Logger?.LogInformation($"{Id} => {nameof(PersistentSubscriptionEventStoreStream)} - StreamId: {_persistentEventStoreStreamConfiguration.StreamId} - GroupId: {_persistentEventStoreStreamConfiguration.GroupId}");

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

        protected override IMessage CreateMessage(IEvent @event, ResolvedEvent resolvedEvent)
        {
            return new EventStorePersistentSubscriptionMessage(@event.EventId, @event, resolvedEvent, _eventStorePersistentSubscription);
        }
    }
}
