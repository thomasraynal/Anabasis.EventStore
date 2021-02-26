using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription
{
  public abstract class BaseCatchupEventStoreCache<TKey, TCacheItem> : BaseEventStoreCache<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {
    private readonly SourceCache<TCacheItem, TKey> _caughtingUpCache = new SourceCache<TCacheItem, TKey>(item => item.EntityId);

    protected BaseCatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor, IEventStoreCacheConfiguration<TKey, TCacheItem> cacheConfiguration, IEventTypeProvider<TKey, TCacheItem> eventTypeProvider, ILogger logger = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, logger)
    {

    }

    protected override void OnDispose()
    {
      _caughtingUpCache.Dispose();
    }

    protected override void OnResolvedEvent(ResolvedEvent @event)
    {

      var cache = IsCaughtUp ? Cache : _caughtingUpCache;

      UpdateCacheState(@event, cache);

    }

    protected abstract EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(IEventStoreConnection connection,
      Func<EventStoreCatchUpSubscription, ResolvedEvent,Task> onEvent, Action<EventStoreCatchUpSubscription> onCaughtUp,
      Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped);

    protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {

      return Observable.Create<ResolvedEvent>(obs =>
      {

        Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent @event)
        {
          obs.OnNext(@event);

          return Task.CompletedTask;
        }

        void onCaughtUp(EventStoreCatchUpSubscription _)
        {

          Cache.Edit(innerCache =>
          {

            innerCache.Load(_caughtingUpCache.Items);

            _caughtingUpCache.Clear();

          });

          IsCaughtUpSubject.OnNext(true);

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

        var subscription = GetEventStoreCatchUpSubscription(connection, onEvent, onCaughtUp, onSubscriptionDropped);

        return Disposable.Create(() =>
        {

          subscription.Stop();

        });

      });
    }

  }
}
