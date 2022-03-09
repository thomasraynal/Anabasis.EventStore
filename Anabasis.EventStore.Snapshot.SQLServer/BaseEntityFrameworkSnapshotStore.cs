using Anabasis.Common;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot.SQLServer
{

    public abstract class BaseEntityFrameworkSnapshotStore<TAggregate, TDbContext, TAggregateSnapshot> : ISnapshotStore<TAggregate>
        where TDbContext : BaseAggregateSnapshotDbContext<TAggregateSnapshot>
        where TAggregate : IAggregate, new()
        where TAggregateSnapshot : class, IAggregateSnapshot, new()
    {

        private readonly IDbContextFactory<TDbContext> _aggregateSnapshotDbContextFactory;

        public BaseEntityFrameworkSnapshotStore(IDbContextFactory<TDbContext> aggregateSnapshotDbContextFactory)
        {
            _aggregateSnapshotDbContextFactory = aggregateSnapshotDbContextFactory;
        }


        public async Task<TAggregate> GetByVersionOrLast(string streamId, string[] eventFilters, int? version = null)
        {

            using var context = _aggregateSnapshotDbContextFactory.CreateDbContext();

            var eventFilter = string.Concat(eventFilters);

            var aggregateSnapshotQueryable = context.AggregateSnapshots.AsQueryable().OrderByDescending(p => p.LastModifiedUtc);

            TAggregateSnapshot aggregateSnapshot = null;

            if (null == version)
            {
                aggregateSnapshot = await aggregateSnapshotQueryable.OrderByDescending(snapshot => snapshot.LastModifiedUtc).FirstOrDefaultAsync(snapshot => snapshot.StreamId == streamId && snapshot.EventFilter == eventFilter);
            }
            else
            {
                aggregateSnapshot = await aggregateSnapshotQueryable.OrderByDescending(snapshot => snapshot.LastModifiedUtc).FirstOrDefaultAsync(snapshot => snapshot.Version == version && snapshot.StreamId == streamId && snapshot.EventFilter == eventFilter);
            }

            if (null == aggregateSnapshot) return default;

            var aggregate = aggregateSnapshot.SerializedAggregate.JsonTo<TAggregate>();

            return aggregate;

        }

        public async Task<TAggregate[]> GetByVersionOrLast(string[] eventFilters, int? version = null)
        {
            using var context = _aggregateSnapshotDbContextFactory.CreateDbContext();

            var eventFilter = string.Concat(eventFilters);

            var isLatest = version == null;

            TAggregateSnapshot[] aggregateSnapshots = null;

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

            if (aggregateSnapshots.Length == 0) return System.Array.Empty<TAggregate>();

            return aggregateSnapshots.Select(aggregateSnapshot => aggregateSnapshot.SerializedAggregate.JsonTo<TAggregate>()).ToArray();

        }

        public async Task Save(string[] eventFilters, TAggregate aggregate)
        {
            using var context = _aggregateSnapshotDbContextFactory.CreateDbContext();

            var aggregateSnapshot = new TAggregateSnapshot
            {
                StreamId = aggregate.EntityId,
                Version = aggregate.Version,
                EventFilter = string.Concat(eventFilters),
                SerializedAggregate = aggregate.ToJson(),
            };

            if (await context.AggregateSnapshots.ContainsAsync(aggregateSnapshot)) return;

            context.AggregateSnapshots.Add(aggregateSnapshot);

            await context.SaveChangesAsync();

        }

        public async Task<TAggregate[]> GetAll()
        {
            var results = new List<TAggregate>();

            using var context = _aggregateSnapshotDbContextFactory.CreateDbContext();

            foreach (var aggregateSnapshot in await context.AggregateSnapshots.AsQueryable().ToListAsync())
            {
                var aggregate = aggregateSnapshot.SerializedAggregate.JsonTo<TAggregate>();

                results.Add(aggregate);
            }

            return results.ToArray();

        }
    }
}
