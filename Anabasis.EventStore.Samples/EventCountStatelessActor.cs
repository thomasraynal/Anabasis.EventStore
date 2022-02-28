using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
    public class EventCountStatelessActor : BaseEventStoreStatelessActor
    {
        public EventCountStatelessActor(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, connectionStatusMonitor, loggerFactory)
        {
        }

        public EventCountStatelessActor(IActorConfigurationFactory actorConfigurationFactory, IEventStoreRepository eventStoreRepository, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
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
