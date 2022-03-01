using Anabasis.Common;
using Anabasis.Common.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
    public class EventCountStatelessActor : BaseStatelessActor
    {
        public EventCountStatelessActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public EventCountStatelessActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
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
