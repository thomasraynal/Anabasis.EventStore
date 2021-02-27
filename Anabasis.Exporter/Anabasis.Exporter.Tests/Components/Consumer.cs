using DynamicData;
using Anabasis.Tests.Demo;
using System;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;

namespace Anabasis.Tests
{
  public class Consumer : IConsumer
  {
    public Consumer(CatchupEventStoreCache<Guid, Item> catchupEventStoreCache)
    {
      OnChange = catchupEventStoreCache.AsObservableCache();
    }

    public IObservableCache<Item, Guid> OnChange { get; }
  }
}
