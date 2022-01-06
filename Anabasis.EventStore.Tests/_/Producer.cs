using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Tests.Demo;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public class Producer : IProducer
    {
        private readonly IEventStoreAggregateRepository _repository;

        public Producer(IEventStoreAggregateRepository repository)
        {
            _repository = repository;
        }

        public string Create()
        {
            var item = new Item();
            _repository.Apply(item, new CreateItemEvent());
            return item.EntityId;
        }

        public void Mutate(Item item, string payload)
        {
            var itemUpdatedEvent = new UpdateItemPayloadEvent()
            {
                Payload = payload
            };

            _repository.Apply(item, itemUpdatedEvent);
        }
    }
}
