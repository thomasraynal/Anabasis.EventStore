using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Queue
{
    public class SubscribeFromEndToOneStreamEventStoreQueue : BaseSubscribeToOneStreamEventStoreQueue
    {
        public SubscribeFromEndToOneStreamEventStoreQueue(IConnectionStatusMonitor connectionMonitor, SubscribeToOneStreamEventStoreQueueConfiguration subscribeToOneStreamEventStoreQueueConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory) : base(StreamPosition.End, connectionMonitor, subscribeToOneStreamEventStoreQueueConfiguration, eventTypeProvider, loggerFactory.CreateLogger<SubscribeFromEndToOneStreamEventStoreQueue>())
        {
        }
    }
}
