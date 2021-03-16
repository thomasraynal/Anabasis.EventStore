using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot
{
  public class InMemorySnapshotStore<TKey, TAggregate> : ISnapshotStore<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {
    private readonly JsonSerializerSettings _jsonSerializerSettings;
    private readonly DbContextOptions<AggregateSnapshotContext> _entityFrameworkOptions;

    class AggregateSnapshotContext : DbContext
    {
      public AggregateSnapshotContext(DbContextOptions<AggregateSnapshotContext> options)
          : base(options)
      { }
      public DbSet<AggregateSnapshot> AggregateSnapshots { get; set; }
    }


    public InMemorySnapshotStore()
    {

      _entityFrameworkOptions = new DbContextOptionsBuilder<AggregateSnapshotContext>()
                      .UseInMemoryDatabase(databaseName: "MockDB")
                      .Options;

      using (var context = new AggregateSnapshotContext(_entityFrameworkOptions))
      {
        var customer = new AggregateSnapshot
        {
          StreamId = "streamId",
          Version = 1,
          EventFilter = "filter-filter",
          SerializedAggregate = "sdfsdfsdsdfsdf"
        };

        context.AggregateSnapshots.Add(customer);
        context.SaveChanges();

      }

      _jsonSerializerSettings = new JsonSerializerSettings()
      {
        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented

      };
    }

    public async Task<TAggregate> Get(string streamId, string eventFilter)
    {
      using var context = new AggregateSnapshotContext(_entityFrameworkOptions);

      var aggregateSnapshot = await context.AggregateSnapshots.AsQueryable().FirstOrDefaultAsync(snapshot => snapshot.StreamId == "streamId");

      var aggregate = JsonConvert.DeserializeObject<TAggregate>(aggregateSnapshot.SerializedAggregate, _jsonSerializerSettings);

      return aggregate;
    }

    public async Task Save(string streamId, string[] eventFilters, TAggregate aggregate)
    {
      using var context = new AggregateSnapshotContext(_entityFrameworkOptions);

      var aggregateSnapshot = new AggregateSnapshot
      {
        StreamId = streamId,
        Version = aggregate.Version,
        EventFilter = string.Concat(eventFilters),
        SerializedAggregate = JsonConvert.SerializeObject(aggregate, _jsonSerializerSettings),
        Id = Guid.ne
      };

      context.AggregateSnapshots.Add(aggregateSnapshot);

      await context.SaveChangesAsync();

    }

    public async Task<TAggregate[]> Get(string[] eventFilters)
    {
      using var context = new AggregateSnapshotContext(_entityFrameworkOptions);

      var filter = string.Concat(eventFilters);

      var aggregateSnapshots = await context.AggregateSnapshots.AsQueryable().Where(snapshot => snapshot.EventFilter == filter).ToArrayAsync();

      if (aggregateSnapshots.Length == 0) return new TAggregate[0];

      return aggregateSnapshots.Select(aggregateSnapshot => JsonConvert.DeserializeObject<TAggregate>(aggregateSnapshot.SerializedAggregate, _jsonSerializerSettings)).ToArray();

    }

    public Task Save(string[] eventFilters, TAggregate aggregate)
    {

      Directory.CreateDirectory(_fileSystemSnapshotStoreConfiguration.RepositoryDirectory);

      var filters = string.Concat(eventFilters);

      var path = Path.Combine(_fileSystemSnapshotStoreConfiguration.RepositoryDirectory, filters, aggregate.StreamId);

      File.WriteAllText(path, JsonConvert.SerializeObject(aggregate, _jsonSerializerSettings));

      return Task.CompletedTask;

    }

  }
}
