using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
    public class FileSystemSnapshotProvider<TKey, TAggregate> : ISnapshotStore<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
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

            var aggregateSnapshot = new AggregateSnapshot
            {
                StreamId = aggregate.StreamId,
                Version = aggregate.Version,
                EventFilter = string.Concat(eventFilters),
                SerializedAggregate = aggregate.ToJson(),
                LastModifiedUtc = DateTime.UtcNow
            };


            File.WriteAllText($"./snapshot_{Guid.NewGuid()}", JsonConvert.SerializeObject(aggregateSnapshot));

            return Task.CompletedTask;

        }
    }
}
