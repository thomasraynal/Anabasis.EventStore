using Anabasis.Common;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Repository
{
    public interface IEventStoreAggregateRepository : IEventStoreRepository
    {
        Task<TAggregate> GetAggregateByStreamId<TAggregate>(string streamId, long? fromVersion, IEventTypeProvider eventTypeProvider, bool loadEvents = false) 
           where TAggregate : class, IAggregate, new();

        Task Apply<TAggregate, TEvent>(TAggregate aggregate, TEvent ev, params KeyValuePair<string, string>[] extraHeaders)
           where TAggregate : class, IAggregate
           where TEvent : IAggregateEvent<TAggregate>;
    }
}
