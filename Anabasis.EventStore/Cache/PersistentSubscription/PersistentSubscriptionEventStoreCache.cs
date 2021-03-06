//using System;
//using System.Reactive.Disposables;
//using System.Reactive.Linq;
//using EventStore.ClientAPI;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Anabasis.EventStore.Shared;
//using Anabasis.EventStore.EventProvider;
//using Anabasis.EventStore.Connection;

//namespace Anabasis.EventStore.Cache
//{
//    public class PersistentSubscriptionEventStoreCache<TKey, TAggregate> : BaseEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
//    {
//        private readonly PersistentSubscriptionCacheConfiguration<TKey, TAggregate> _persistentSubscriptionCacheConfiguration;
//        private EventStorePersistentSubscriptionBase _eventStorePersistentSubscription;

//        public PersistentSubscriptionEventStoreCache(IConnectionStatusMonitor connectionMonitor,
//          PersistentSubscriptionCacheConfiguration<TKey, TAggregate> persistentSubscriptionCacheConfiguration,
//          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
//          ILogger<PersistentSubscriptionEventStoreCache<TKey, TAggregate>> logger = null) : base(connectionMonitor, persistentSubscriptionCacheConfiguration, eventTypeProvider, null, null, logger)
//        {
//            _persistentSubscriptionCacheConfiguration = persistentSubscriptionCacheConfiguration;

//            InitializeAndRun();
//        }

//        public void Acknowledge(ResolvedEvent resolvedEvent)
//        {
//            _eventStorePersistentSubscription.Acknowledge(resolvedEvent);
//        }

//        public void NotAcknowledge(ResolvedEvent resolvedEvent, PersistentSubscriptionNakEventAction persistentSubscriptionNakEventAction, string reason = null)
//        {
//            _eventStorePersistentSubscription.Fail(resolvedEvent, persistentSubscriptionNakEventAction, reason);
//        }

//        protected override void OnResolvedEvent(ResolvedEvent @event)
//        {
//            try
//            {
//                base.OnResolvedEvent(@event);
//            }
//            catch (Exception exception)
//            {
//                base.NotAcknowledge(@event);

//            }
//        }

//        protected override void OnInitialize(bool isConnected)
//        {
//            IsCaughtUpSubject.OnNext(true);

//            Cache.Edit((innerCache) =>
//            {
//                innerCache.Clear();
//            });
//        }

//        protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
//        {
//            return Observable.Create<ResolvedEvent>(async observer =>
//            {

//                Task onEvent(EventStorePersistentSubscriptionBase _, ResolvedEvent resolvedEvent)
//                {
//                    observer.OnNext(resolvedEvent);

//                    return Task.CompletedTask;
//                }

//                void onSubscriptionDropped(EventStorePersistentSubscriptionBase _, SubscriptionDropReason subscriptionDropReason, Exception exception)
//                {
//                    switch (subscriptionDropReason)
//                    {
//                        case SubscriptionDropReason.UserInitiated:
//                        case SubscriptionDropReason.ConnectionClosed:
//                            break;

//                        case SubscriptionDropReason.NotAuthenticated:
//                        case SubscriptionDropReason.AccessDenied:
//                        case SubscriptionDropReason.SubscribingError:
//                        case SubscriptionDropReason.ServerError:
//                        case SubscriptionDropReason.CatchUpError:
//                        case SubscriptionDropReason.ProcessingQueueOverflow:
//                        case SubscriptionDropReason.EventHandlerException:
//                        case SubscriptionDropReason.MaxSubscribersReached:
//                        case SubscriptionDropReason.PersistentSubscriptionDeleted:
//                        case SubscriptionDropReason.Unknown:
//                        case SubscriptionDropReason.NotFound:

//                            throw new InvalidOperationException($"{nameof(SubscriptionDropReason)} {subscriptionDropReason} throwed the consumer in a invalid state");

//                        default:

//                            throw new InvalidOperationException($"{nameof(SubscriptionDropReason)} {subscriptionDropReason} not found");
//                    }
//                }

//                _eventStorePersistentSubscription = await connection.ConnectToPersistentSubscriptionAsync(
//                   _persistentSubscriptionCacheConfiguration.StreamId,
//                   _persistentSubscriptionCacheConfiguration.GroupId,
//                   eventAppeared: onEvent,
//                   subscriptionDropped: onSubscriptionDropped,
//                   userCredentials: _eventStoreCacheConfiguration.UserCredentials,
//                   _persistentSubscriptionCacheConfiguration.BufferSize,
//                   _persistentSubscriptionCacheConfiguration.AutoAck);

//                return Disposable.Create(() =>
//          {
//              _eventStorePersistentSubscription.Stop(TimeSpan.FromSeconds(5));
//          });

//            });
//        }
//    }
//}
