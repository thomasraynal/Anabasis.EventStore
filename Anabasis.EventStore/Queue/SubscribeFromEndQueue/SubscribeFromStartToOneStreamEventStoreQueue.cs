using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Queue
{
    public class SubscribeFromStartToOneStreamEventStoreQueue : BaseSubscribeToOneStreamEventStoreQueue
    {
        public SubscribeFromStartToOneStreamEventStoreQueue(IConnectionStatusMonitor connectionMonitor, SubscribeToOneStreamEventStoreQueueConfiguration subscribeToOneStreamEventStoreQueueConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory) : base(
            StreamPosition.Start, 
            connectionMonitor, 
            subscribeToOneStreamEventStoreQueueConfiguration, 
            eventTypeProvider,
            loggerFactory.CreateLogger<SubscribeFromStartToOneStreamEventStoreQueue>())
        {
        }
    }
}
