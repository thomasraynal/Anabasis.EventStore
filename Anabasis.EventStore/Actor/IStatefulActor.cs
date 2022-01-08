using Anabasis.Common;
using Anabasis.EventStore.Cache;

namespace Anabasis.EventStore.Actor
{
    public interface IStatefulActor<TAggregate> : IStatelessActor where TAggregate : IAggregate, new()
    {
        IEventStoreCache<TAggregate> State { get; }
    }
}
