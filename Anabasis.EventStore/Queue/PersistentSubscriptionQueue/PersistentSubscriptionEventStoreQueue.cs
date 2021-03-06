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
    public class PersistentSubscriptionEventStoreQueue : BaseEventStoreQueue
    {
        private readonly PersistentSubscriptionEventStoreQueueConfiguration _persistentEventStoreQueueConfiguration;
        private EventStorePersistentSubscriptionBase _eventStorePersistentSubscription;

        public PersistentSubscriptionEventStoreQueue(IConnectionStatusMonitor connectionMonitor,
          PersistentSubscriptionEventStoreQueueConfiguration persistentEventStoreQueueConfiguration,
          IEventTypeProvider eventTypeProvider,
          ILogger<PersistentSubscriptionEventStoreQueue> logger = null) : base(connectionMonitor, persistentEventStoreQueueConfiguration, eventTypeProvider, logger)
        {
            _persistentEventStoreQueueConfiguration = persistentEventStoreQueueConfiguration;

            InitializeAndRun();

        }

        public void Acknowledge(ResolvedEvent resolvedEvent)
        {
            _eventStorePersistentSubscription.Acknowledge(resolvedEvent);
        }

        public void NotAcknowledge(ResolvedEvent resolvedEvent, PersistentSubscriptionNakEventAction persistentSubscriptionNakEventAction, string reason = null)
        {
            _eventStorePersistentSubscription.Fail(resolvedEvent, persistentSubscriptionNakEventAction, reason);
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

                _eventStorePersistentSubscription = await connection.ConnectToPersistentSubscriptionAsync(
                 _persistentEventStoreQueueConfiguration.StreamId,
                 _persistentEventStoreQueueConfiguration.GroupId,
                 eventAppeared: onEvent,
                 subscriptionDropped: onSubscriptionDropped,
                 userCredentials: _persistentEventStoreQueueConfiguration.UserCredentials,
                 _persistentEventStoreQueueConfiguration.BufferSize,
                 _persistentEventStoreQueueConfiguration.AutoAck);

                return Disposable.Create(() =>
                  {
                      _eventStorePersistentSubscription.Stop(TimeSpan.FromSeconds(5));
                  });

            });
        }
    }
}
