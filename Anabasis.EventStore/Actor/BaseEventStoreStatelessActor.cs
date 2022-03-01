using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.Common.Configuration;
using EventStore.ClientAPI;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseEventStoreStatelessActor : BaseStatelessActor, IEventStoreStatelessActor
    {
        protected BaseEventStoreStatelessActor(IActorConfiguration actorConfiguration, 
            IEventStoreRepository eventStoreRepository,
            IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor,
            ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
            Initialize(eventStoreRepository, connectionStatusMonitor);
        }

        protected BaseEventStoreStatelessActor(IActorConfigurationFactory actorConfigurationFactory,
            IEventStoreRepository eventStoreRepository,
            IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor,
            ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
            Initialize(eventStoreRepository, connectionStatusMonitor);
        }

        public void Initialize(IEventStoreRepository eventStoreRepository, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ILoggerFactory loggerFactory = null)
        {
            ConnectTo(new EventStoreBus(connectionStatusMonitor, eventStoreRepository, loggerFactory)).Wait();
        }

    }
}
