using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseCatchupEventStoreCache<TKey, TAggregate> : BaseEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        protected SourceCache<TAggregate, TKey> CaughtingUpCache { get; } = new SourceCache<TAggregate, TKey>(item => item.EntityId);
        protected readonly ManualResetEventSlim _blockEventConsumption;

        protected BaseCatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
          IEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          ILoggerFactory loggerFactory,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null) :
          base(connectionMonitor, cacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            _blockEventConsumption = new ManualResetEventSlim(true);
        }

        protected abstract EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(IEventStoreConnection connection,
          Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent, Action<EventStoreCatchUpSubscription> onCaughtUp,
          Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped);

        protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
        {

            return Observable.Create<ResolvedEvent>(obs =>
            {

                async Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent @event)
                {
                    _blockEventConsumption.Wait();

                    Logger?.LogDebug($"{Id} => OnEvent {@event.Event.EventType} - v.{@event.Event.EventNumber}");

                    obs.OnNext(@event);

                    if (_eventStoreCacheConfiguration.UseSnapshot)
                    {
                        foreach (var aggregate in Cache.Items)
                        {
                            if (_snapshotStrategy.IsSnapShotRequired(aggregate))
                            {
                                Logger?.LogInformation($"{Id} => Snapshoting aggregate => {aggregate.EntityId} {aggregate.GetType()} - v.{aggregate.Version}");

                                var eventFilter = GetEventsFilters();

                                aggregate.VersionSnapshot = aggregate.Version;

                                await _snapshotStore.Save(eventFilter, aggregate);

                            }
                        }
                    }
                }

                void onCaughtUp(EventStoreCatchUpSubscription _)
                {
                    Logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");

                    //event consumption should be sequential caughtUpEnd -> next event, so this is just to be sure...
                    _blockEventConsumption.Reset();

                    //this handle a caughting up NOT due to disconnection, i.e a caughting up due to a lag
                    if (!IsCaughtUp)
                    {

                        Cache.Edit(innerCache =>
                       {
                           Logger?.LogInformation($"{Id} => OnCaughtUp - switch from CaughtingUpCache");

                           innerCache.Load(CaughtingUpCache.Items);

                           CaughtingUpCache.Clear();

                       });

                        IsCaughtUpSubject.OnNext(true);

                    }

                    Logger?.LogInformation($"{Id} => OnCaughtUp - IsCaughtUp: {IsCaughtUp}");

                    _blockEventConsumption.Set();

                }

                void onSubscriptionDropped(EventStoreCatchUpSubscription _, SubscriptionDropReason subscriptionDropReason, Exception exception)
                {
                    switch (subscriptionDropReason)
                    {
                        case SubscriptionDropReason.UserInitiated:
                        case SubscriptionDropReason.ConnectionClosed:
                            break;

                        case SubscriptionDropReason.CatchUpError:
                            Logger?.LogInformation($"{nameof(SubscriptionDropReason)} - {subscriptionDropReason}", exception);
                            break;

                        case SubscriptionDropReason.NotAuthenticated:
                        case SubscriptionDropReason.AccessDenied:
                        case SubscriptionDropReason.SubscribingError:
                        case SubscriptionDropReason.ServerError:
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

                var subscription = GetEventStoreCatchUpSubscription(connection, onEvent, onCaughtUp, onSubscriptionDropped);

                return Disposable.Create(() =>
                  {
                      subscription.Stop();
                  });

            });
        }
        public string[] GetEventsFilters()
        {
            var eventTypeFilters = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

            return eventTypeFilters;
        }

        public override void Dispose()
        {

            CaughtingUpCache.Dispose();

            base.Dispose();
        }

    }
}
