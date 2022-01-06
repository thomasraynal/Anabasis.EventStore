using DynamicData;
using System;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Tests.Demo;

namespace Anabasis.EventStore.Tests
{
  public class Consumer : IConsumer
  {
    public Consumer(AllStreamsCatchupCache<Item> catchupEventStoreCache)
    {
      OnChange = catchupEventStoreCache.AsObservableCache();
    }

    public IObservableCache<Item, string> OnChange { get; }
  }
}
