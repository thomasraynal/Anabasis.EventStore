using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Snapshot.InMemory;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using Anabasis.Common;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
    public class FileSystemSnapshotProvider<TAggregate> : ISnapshotStore<TAggregate> where TAggregate : IAggregate, new()
    {

        public Task<TAggregate[]> GetAll()
        {
            throw new NotImplementedException();
        }

        public Task<TAggregate[]> GetByVersionOrLast(string[] eventFilters, int? version = null)
        {
            throw new NotImplementedException();
        }

        public Task<TAggregate> GetByVersionOrLast(string streamId, string[] eventFilters, int? version = null)
        {
            var snapshots = Directory.EnumerateFiles(Directory.GetCurrentDirectory())
                                .Where(file => file.Contains("snapshot_"))
                                .ToArray();

            if (snapshots.Length == 0) return Task.FromResult<TAggregate>(default);


            var last = snapshots.OrderByDescending((file) => new FileInfo(file).CreationTimeUtc)
                                .FirstOrDefault();

            var aggregateSnapshot = JsonConvert.DeserializeObject<AggregateSnapshot>(File.ReadAllText(last));

            return Task.FromResult(JsonConvert.DeserializeObject<TAggregate>(aggregateSnapshot.SerializedAggregate));
        }

        public Task Save(string[] eventFilters, TAggregate aggregate)
        {

            var aggregateSnapshot = new AggregateSnapshot(aggregate.EntityId, string.Concat(eventFilters), aggregate.Version, aggregate.ToJson(), DateTime.UtcNow);

            File.WriteAllText($"./snapshot_{Guid.NewGuid()}", JsonConvert.SerializeObject(aggregateSnapshot));

            return Task.CompletedTask;

        }
    }
}
