using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Repository;

namespace Anabasis.EventStore.Demo
{
  public class MarketDataSink : BaseAggregateActor<string, MarketData>
  {
    public MarketDataSink(IEventStoreAggregateRepository<string> eventStoreRepository, IEventStoreCache<string, MarketData> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
    {
    }
  }
}
