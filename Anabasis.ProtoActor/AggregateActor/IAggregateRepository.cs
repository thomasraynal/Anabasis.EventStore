using Anabasis.Common;
using Anabasis.EventStore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.AggregateActor
{
    public interface IAggregateRepository<TAggregate> where TAggregate : class, IAggregate, new()
    {
        Task<TAggregate> GetAggregateByStreamIdFromVersion(string streamId, long? version, IEventTypeProvider eventTypeProvider, bool useSnapshot = false, bool keepEventsOnAggregate = false);
        Task Save(TAggregate aggregate, bool useSnapshot = false);
    }
}
