using Anabasis.Common;
using Anabasis.EventStore.Cache;

namespace Anabasis.EventStore.Actor
{
    public interface IEventStoreStatefulActor<TAggregate> : IEventStoreStatelessActor where TAggregate : IAggregate, new()
    {
        IEventStoreCache<TAggregate> State { get; }
    }
}
