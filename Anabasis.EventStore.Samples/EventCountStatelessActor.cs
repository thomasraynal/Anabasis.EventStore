using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
    public class EventCountStatelessActor : BaseStatelessActor
    {
        public EventCountStatelessActor(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
        {
        }

        public Task Handle( EventCountOne eventCountOne)
        {
            Console.WriteLine($"{Id} received {nameof(EventCountOne)}");

            return Task.CompletedTask;
        }

        public Task Handle(EventCountTwo eventCountTwo)
        {
            Console.WriteLine($"{Id} received {nameof(EventCountTwo)}");

            return Task.CompletedTask;
        }

   
    }
}
