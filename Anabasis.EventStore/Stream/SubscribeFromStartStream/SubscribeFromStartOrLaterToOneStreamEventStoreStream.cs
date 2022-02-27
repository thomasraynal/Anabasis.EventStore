using Anabasis.Common;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeFromStartOrLaterToOneStreamEventStoreStream : BaseSubscribeToOneStreamEventStoreStream
    {
        public SubscribeFromStartOrLaterToOneStreamEventStoreStream(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration subscribeToOneStreamEventStoreStreamConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null) : base(
            subscribeToOneStreamEventStoreStreamConfiguration.EventStreamPosition, 
            connectionMonitor, 
            subscribeToOneStreamEventStoreStreamConfiguration, 
            eventTypeProvider,
            loggerFactory.CreateLogger<SubscribeFromStartOrLaterToOneStreamEventStoreStream>())
        {
        }
    }
}
