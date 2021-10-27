using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeFromStartOrLaterToOneStreamEventStoreStream : BaseSubscribeToOneStreamEventStoreStream
    {
        public SubscribeFromStartOrLaterToOneStreamEventStoreStream(IConnectionStatusMonitor connectionMonitor, SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration subscribeToOneStreamEventStoreStreamConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null) : base(
            subscribeToOneStreamEventStoreStreamConfiguration.EventStreamPosition, 
            connectionMonitor, 
            subscribeToOneStreamEventStoreStreamConfiguration, 
            eventTypeProvider,
            loggerFactory.CreateLogger<SubscribeFromStartOrLaterToOneStreamEventStoreStream>())
        {
        }
    }
}
