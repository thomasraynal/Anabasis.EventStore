using Anabasis.Common;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeFromEndToOneStreamEventStoreStream : BaseSubscribeToOneStreamEventStoreStream
    {
        public SubscribeFromEndToOneStreamEventStoreStream(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration subscribeToOneStreamEventStoreStreamConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory? loggerFactory = null) : base(0, connectionMonitor, subscribeToOneStreamEventStoreStreamConfiguration, eventTypeProvider, loggerFactory?.CreateLogger<SubscribeFromEndToOneStreamEventStoreStream>())
        {
        }
    }
}
