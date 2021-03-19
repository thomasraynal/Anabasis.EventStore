using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
        modelBuilder.Entity<AggregateSnapshot>().HasKey(aggregateSnapshot => new
        {
          aggregateSnapshot.StreamId,
          aggregateSnapshot.EventFilter,
          aggregateSnapshot.Version
        });
      }

      public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
      {

        var now = DateTime.UtcNow;

        var entries = ChangeTracker
            .Entries()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in entries)
        {
          if (entry.Entity is AggregateSnapshot aggregateSnapshot)
          {
            aggregateSnapshot.LastModifiedUtc = now;
          }
        }

        return base.SaveChangesAsync();
      }

    }


    public InMemorySnapshotStore()
    {

      _entityFrameworkOptions = new DbContextOptionsBuilder<AggregateSnapshotContext>()
                      .UseInMemoryDatabase(databaseName: "AggregateSnapshots")
                      .Options;


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

    public async Task<TAggregate> Get(string streamId, string[] eventFilters, int? version = null)
    {

      using var context = new AggregateSnapshotContext(_entityFrameworkOptions);

      var eventFilter = string.Concat(eventFilters);

      var aggregateSnapshotQueryable = context.AggregateSnapshots.AsQueryable().OrderByDescending(p => p.LastModifiedUtc);

      AggregateSnapshot aggregateSnapshot = null;

      if (null == version)
      {
        aggregateSnapshot = await aggregateSnapshotQueryable.OrderByDescending(snapshot => snapshot.LastModifiedUtc).FirstOrDefaultAsync(snapshot => snapshot.StreamId == streamId && snapshot.EventFilter == eventFilter);
      }
      else
      {
        aggregateSnapshot = await aggregateSnapshotQueryable.OrderByDescending(snapshot => snapshot.LastModifiedUtc).FirstOrDefaultAsync(snapshot => snapshot.Version == version && snapshot.StreamId == streamId && snapshot.EventFilter == eventFilter);
      }

      if (null == aggregateSnapshot) return default;

      var aggregate = JsonConvert.DeserializeObject<TAggregate>(aggregateSnapshot.SerializedAggregate, _jsonSerializerSettings);

      return aggregate;

    }

    public async Task<TAggregate[]> Get(string[] eventFilters, int? version = null)
    {
      using var context = new AggregateSnapshotContext(_entityFrameworkOptions);

      var eventFilter = string.Concat(eventFilters);

      var isLatest = version == null;

      AggregateSnapshot[] aggregateSnapshots = null;

      if (isLatest)
      {
       
        var orderByDescendingQueryable = context.AggregateSnapshots.AsQueryable().OrderByDescending(snapshot => snapshot.LastModifiedUtc);

        //https://github.com/dotnet/efcore/issues/13805
        aggregateSnapshots = await context.AggregateSnapshots.AsQueryable()
                                                            .Where(snapshot => snapshot.EventFilter == eventFilter)
                                                            .OrderByDescending(snapshot => snapshot.LastModifiedUtc)
                                                            .Select(snapshot => snapshot.StreamId)
                                                            .Distinct()
                                                            .SelectMany(snapshot => orderByDescendingQueryable.Where(b => b.StreamId == snapshot).Take(1), (streamId, aggregateSnapshot) => aggregateSnapshot)
                                                            .ToArrayAsync();
      }
      else
      {
        aggregateSnapshots = await context.AggregateSnapshots.AsQueryable().Where(snapshot => snapshot.EventFilter == eventFilter && snapshot.Version == version).ToArrayAsync();
      }

      if (aggregateSnapshots.Length == 0) return new TAggregate[0];

      return aggregateSnapshots.Select(aggregateSnapshot => JsonConvert.DeserializeObject<TAggregate>(aggregateSnapshot.SerializedAggregate, _jsonSerializerSettings)).ToArray();

    }

    public async Task Save(string[] eventFilters, TAggregate aggregate)
    {
      using var context = new AggregateSnapshotContext(_entityFrameworkOptions);

      var aggregateSnapshot = new AggregateSnapshot
      {
        StreamId = aggregate.StreamId,
        Version = aggregate.Version,
        EventFilter = string.Concat(eventFilters),
        SerializedAggregate = JsonConvert.SerializeObject(aggregate, _jsonSerializerSettings),
      };
      
      context.AggregateSnapshots.Add(aggregateSnapshot);

      await context.SaveChangesAsync();

    }

    public async Task<TAggregate[]> GetAll()
    {
      var results = new List<TAggregate>();

      using var context = new AggregateSnapshotContext(_entityFrameworkOptions);

      foreach (var aggregateSnapshot in await context.AggregateSnapshots.AsQueryable().ToListAsync())
      {
        var aggregate = JsonConvert.DeserializeObject<TAggregate>(aggregateSnapshot.SerializedAggregate, _jsonSerializerSettings);

        results.Add(aggregate);
      }

      return results.ToArray();

    }
  }
}
