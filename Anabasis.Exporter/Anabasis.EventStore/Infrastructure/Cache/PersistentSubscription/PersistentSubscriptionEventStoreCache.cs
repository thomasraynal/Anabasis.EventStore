using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using EventStore.ClientAPI;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Anabasis.EventStore.Infrastructure.Cache;

namespace Anabasis.EventStore.Infrastructure
{
  public class PersistentSubscriptionEventStoreCache<TKey, TAggregate> : BaseEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {
    private readonly PersistentSubscriptionCacheConfiguration<TKey, TAggregate> _persistentSubscriptionCacheConfiguration;

    public PersistentSubscriptionEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      PersistentSubscriptionCacheConfiguration<TKey, TAggregate> persistentSubscriptionCacheConfiguration,
      IEventTypeProvider eventTypeProvider,
      ILogger logger = null) : base(connectionMonitor, persistentSubscriptionCacheConfiguration, eventTypeProvider, logger)
    {
      _persistentSubscriptionCacheConfiguration = persistentSubscriptionCacheConfiguration;

      InitializeAndRun();
    }

    protected override void OnInitialize(bool isConnected)
    {
      IsCaughtUpSubject.OnNext(true);

      Cache.Edit((innerCache) =>
      {
        innerCache.Clear();
      });
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

        var subscription =  await connection.ConnectToPersistentSubscriptionAsync(
         _persistentSubscriptionCacheConfiguration.StreamId,
         _persistentSubscriptionCacheConfiguration.GroupId,
         eventAppeared: onEvent,
         subscriptionDropped: onSubscriptionDropped,
         userCredentials: _eventStoreCacheConfiguration.UserCredentials,
         _persistentSubscriptionCacheConfiguration.BufferSize,
         _persistentSubscriptionCacheConfiguration.AutoAck);

        return Disposable.Create(() =>
        {
          subscription.Stop(TimeSpan.FromSeconds(5));
        });

      });
    }
  }
}
