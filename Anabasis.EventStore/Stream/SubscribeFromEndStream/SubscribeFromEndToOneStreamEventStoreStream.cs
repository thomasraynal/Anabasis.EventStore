using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeFromEndToOneStreamEventStoreStream : BaseSubscribeToOneStreamEventStoreStream
    {
        public SubscribeFromEndToOneStreamEventStoreStream(IConnectionStatusMonitor connectionMonitor, SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration subscribeToOneStreamEventStoreStreamConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null) : base(0, connectionMonitor, subscribeToOneStreamEventStoreStreamConfiguration, eventTypeProvider, loggerFactory.CreateLogger<SubscribeFromEndToOneStreamEventStoreStream>())
        {
        }
    }
}
