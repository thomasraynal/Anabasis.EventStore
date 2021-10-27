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

        public void NotAcknowledge(ResolvedEvent resolvedEvent, PersistentSubscriptionNakEventAction persistentSubscriptionNakEventAction, string reason = null)
        {
            Logger?.LogDebug($"{Id} => NACK event - {resolvedEvent.Event.EventId} - reason : {reason}");

            _eventStorePersistentSubscription.Fail(resolvedEvent, persistentSubscriptionNakEventAction, reason);
        }

        protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
        {
            return Observable.Create<ResolvedEvent>(async observer =>
            {

                Task onEvent(EventStorePersistentSubscriptionBase _, ResolvedEvent resolvedEvent)
                {
                    Logger?.LogDebug($"{Id} => onEvent - {resolvedEvent.Event.EventId}");

                    observer.OnNext(resolvedEvent);

                    return Task.CompletedTask;
                }

                void onSubscriptionDropped(EventStorePersistentSubscriptionBase _, SubscriptionDropReason subscriptionDropReason, Exception exception)
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

                Logger?.LogInformation($"{Id} => ConnectToEventStream - ConnectToPersistentSubscriptionAsync - StreamId: {_persistentEventStoreStreamConfiguration.StreamId} - GroupId: {_persistentEventStoreStreamConfiguration.GroupId}");

                _eventStorePersistentSubscription = await connection.ConnectToPersistentSubscriptionAsync(
                 _persistentEventStoreStreamConfiguration.StreamId,
                 _persistentEventStoreStreamConfiguration.GroupId,
                 eventAppeared: onEvent,
                 subscriptionDropped: onSubscriptionDropped,
                 userCredentials: _persistentEventStoreStreamConfiguration.UserCredentials,
                 _persistentEventStoreStreamConfiguration.BufferSize,
                 _persistentEventStoreStreamConfiguration.AutoAck);

                return Disposable.Create(() =>
                  {
                      _eventStorePersistentSubscription.Stop(TimeSpan.FromSeconds(5));
                  });

            });
        }
    }
}
