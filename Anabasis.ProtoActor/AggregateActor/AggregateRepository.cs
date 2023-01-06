using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.AggregateActor
{
    public class AggregateRepository<TAggregate>: IAggregateRepository<TAggregate> where TAggregate : class, IAggregate, new()
    {
        private readonly IEventStoreAggregateRepository _eventStoreAggregateRepository;

        public AggregateRepository(
            IEventStoreAggregateRepository eventStoreAggregateRepository,
            ISnapshotStore<TAggregate>? snapshotStore = null, 
            ISnapshotStrategy? snapshotStrategy = null)
        {
            _eventStoreAggregateRepository = eventStoreAggregateRepository;
        }
        public Task<TAggregate> GetAggregateByStreamIdFromVersion(string streamId, long? version, IEventTypeProvider eventTypeProvider, bool useSnapshot = false, bool keepEventsOnAggregate = false)
        {
            return _eventStoreAggregateRepository.GetAggregateByStreamId<TAggregate>(streamId, version, eventTypeProvider, keepEventsOnAggregate);
        }

        public Task Save(TAggregate aggregate, bool useSnapshot = false)
        {
            throw new NotImplementedException();
        }
    }
}
