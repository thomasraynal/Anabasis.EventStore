using Anabasis.EventStore.Repository;
using Anabasis.Tests.Demo;
using System;
using System.Threading.Tasks;

namespace Anabasis.Tests
{
  public class Producer : IProducer
    {
        private readonly IEventStoreAggregateRepository<Guid> _repository;

        public Producer(IEventStoreAggregateRepository<Guid> repository)
        {
            _repository = repository;
        }

        public Guid Create()
        {
            var item = new Item();
             _repository.Apply(item, new CreateItemEvent());
            return item.EntityId;
        }

        public async Task<Item> Get(Guid item, bool loadEvents)
        {
            return await _repository.GetById<Item>(item, loadEvents);
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
