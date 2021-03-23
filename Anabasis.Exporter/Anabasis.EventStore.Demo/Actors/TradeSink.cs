using Anabasis.Actor;
using Anabasis.EventStore.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
  public class TradeSink : BaseAggregateActor<long, Trade>
  {
    public TradeSink(IEventStoreAggregateRepository<long> eventStoreRepository, IEventStoreCache<long, Trade> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
    {
    }
  }
}
