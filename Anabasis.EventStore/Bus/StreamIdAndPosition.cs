using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventStoreStreamPosition = EventStore.ClientAPI.StreamPosition;

namespace Anabasis.EventStore.Bus
{
    public class StreamIdAndPosition
    {
        public StreamIdAndPosition(string streamId, long position = EventStoreStreamPosition.Start)
        {
            StreamId = streamId;
            StreamPosition = position;
        }

        public string StreamId { get; }
        public long StreamPosition { get; }

        public override string? ToString()
        {
            return $"{StreamId} - {StreamPosition}";
        }
    }
}
