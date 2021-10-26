using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Repository;

namespace Anabasis.EventStore.Demo
{
    public class TradeSink : BaseStatefulActor<long, Trade>
    {
        public TradeSink(IEventStoreAggregateRepository<long> eventStoreRepository, IEventStoreCache<long, Trade> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
        {
        }
    }
}
