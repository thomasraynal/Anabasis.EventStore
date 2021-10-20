using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Queue
{
    public class SubscribeFromStartOrLaterToOneStreamEventStoreQueue : BaseSubscribeToOneStreamEventStoreQueue
    {
        public SubscribeFromStartOrLaterToOneStreamEventStoreQueue(IConnectionStatusMonitor connectionMonitor, SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration subscribeToOneStreamEventStoreQueueConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null) : base(
            subscribeToOneStreamEventStoreQueueConfiguration.EventStreamPosition, 
            connectionMonitor, 
            subscribeToOneStreamEventStoreQueueConfiguration, 
            eventTypeProvider,
            loggerFactory.CreateLogger<SubscribeFromStartOrLaterToOneStreamEventStoreQueue>())
        {
        }
    }
}
