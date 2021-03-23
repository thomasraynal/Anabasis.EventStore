using Anabasis.Actor;
using Anabasis.EventStore.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
  public class MarketDataSink : BaseAggregateActor<string, MarketData>
  {
    public MarketDataSink(IEventStoreAggregateRepository<string> eventStoreRepository, IEventStoreCache<string, MarketData> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
    {
    }
  }
}
