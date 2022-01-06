using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Actor
{
    public interface IStatefulActor<TAggregate> : IStatelessActor where TAggregate : IAggregate, new()
    {
        IEventStoreCache<TAggregate> State { get; }
    }
}
