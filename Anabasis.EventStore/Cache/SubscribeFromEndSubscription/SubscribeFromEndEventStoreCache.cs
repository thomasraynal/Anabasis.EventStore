using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Cache
{
    public class SubscribeFromEndEventStoreCache<TKey, TAggregate> : BaseEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {

        private readonly SubscribeFromEndCacheConfiguration<TKey, TAggregate> _volatileEventStoreCacheConfiguration;

        public SubscribeFromEndEventStoreCache(IConnectionStatusMonitor connectionMonitor,
          SubscribeFromEndCacheConfiguration<TKey, TAggregate> volatileEventStoreCacheConfiguration,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          ILoggerFactory loggerFactory) : base(connectionMonitor, volatileEventStoreCacheConfiguration, eventTypeProvider, loggerFactory, null, null)
        {
            _volatileEventStoreCacheConfiguration = volatileEventStoreCacheConfiguration;
        }

        protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
        {

            return Observable.Create<ResolvedEvent>(observer =>
          {

              Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent resolvedEvent)
              {
                  observer.OnNext(resolvedEvent);

                  return Task.CompletedTask;
              }

              void onSubscriptionDropped(EventStoreCatchUpSubscription _, SubscriptionDropReason subscriptionDropReason, Exception exception)
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

              void onCaughtUp(EventStoreCatchUpSubscription _)
              {
                  if (IsCaughtUp) return;

                  Logger?.LogInformation($"{Id} => onCaughtUp - IsCaughtUp: {IsCaughtUp}");

                  Cache.Edit((cache) =>
                 {
                     Logger?.LogInformation($"{Id} => onCaughtUp - clear cache");

                     cache.Clear();
                 });

                  IsCaughtUpSubject.OnNext(true);

                  Logger?.LogInformation($"{Id} => onCaughtUp - IsCaughtUp: {IsCaughtUp}");
              }

              var eventTypeFilter = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

              var filter = Filter.EventType.Prefix(eventTypeFilter);

              Logger?.LogInformation($"{Id} => ConnectToEventStream - FilteredSubscribeToAllFrom - Filters [{string.Join("|", eventTypeFilter)}]");

              var subscription = connection.FilteredSubscribeToAllFrom(
               Position.End,
               filter,
               _volatileEventStoreCacheConfiguration.CatchUpSubscriptionFilteredSettings,
               eventAppeared: onEvent,
               liveProcessingStarted: onCaughtUp,
               subscriptionDropped: onSubscriptionDropped,
               userCredentials: _eventStoreCacheConfiguration.UserCredentials);

              return Disposable.Create(() =>
             {
                 subscription.Stop();
             });

          });
        }
    }
}
