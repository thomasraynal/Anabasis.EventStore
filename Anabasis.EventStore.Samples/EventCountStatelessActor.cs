using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
    public class EventCountStatelessActor : BaseEventStoreStatelessActor
    {
        public EventCountStatelessActor(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
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
