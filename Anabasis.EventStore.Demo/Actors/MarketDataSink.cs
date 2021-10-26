using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Repository;

namespace Anabasis.EventStore.Demo
{
    public class MarketDataSink : BaseStatefulActor<string, MarketData>
    {
        public MarketDataSink(IEventStoreAggregateRepository<string> eventStoreRepository, IEventStoreCache<string, MarketData> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
        {
        }
    }
}
