using Proto.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{
    public class EventStoreProvider : IProvider
    {
        public Task DeleteEventsAsync(string actorName, long inclusiveToIndex)
        {
            throw new NotImplementedException();
        }

        public Task DeleteSnapshotsAsync(string actorName, long inclusiveToIndex)
        {
            throw new NotImplementedException();
        }

        public Task<long> GetEventsAsync(string actorName, long indexStart, long indexEnd, Action<object> callback)
        {
            throw new NotImplementedException();
        }

        public Task<(object? Snapshot, long Index)> GetSnapshotAsync(string actorName)
        {
            throw new NotImplementedException();
        }

        public Task<long> PersistEventAsync(string actorName, long index, object @event)
        {
            throw new NotImplementedException();
        }

        public Task PersistSnapshotAsync(string actorName, long index, object snapshot)
        {
            throw new NotImplementedException();
        }
    }
}
