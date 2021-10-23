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
        public SubscribeFromEndToOneStreamEventStoreQueue(IConnectionStatusMonitor connectionMonitor, SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration subscribeToOneStreamEventStoreQueueConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null) : base(0, connectionMonitor, subscribeToOneStreamEventStoreQueueConfiguration, eventTypeProvider, loggerFactory.CreateLogger<SubscribeFromEndToOneStreamEventStoreQueue>())
        {
        }
    }
}
