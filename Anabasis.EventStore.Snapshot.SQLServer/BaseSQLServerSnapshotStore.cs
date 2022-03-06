using Anabasis.Common;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot.SQLServer
{

    public abstract class BaseSQLServerSnapshotStore<TAggregate,TDbContext, TAggregateSnapshot> : ISnapshotStore<TAggregate> 
        where TDbContext: BaseAggregateSnapshotDbContext<TAggregateSnapshot>
        where TAggregate : IAggregate, new()
        where TAggregateSnapshot : AggregateSnapshot, new()
    {
        private readonly SQLServerSnapshotStoreOptions _sQLServerSnapshotStoreOptions;

        public BaseSQLServerSnapshotStore(SQLServerSnapshotStoreOptions sQLServerSnapshotStoreOptions)
        {
            sQLServerSnapshotStoreOptions.Validate();

            _sQLServerSnapshotStoreOptions = sQLServerSnapshotStoreOptions;
        }

        public abstract TDbContext GetDbContext(SQLServerSnapshotStoreOptions sQLServerSnapshotStoreOptions);

        public async Task<TAggregate> GetByVersionOrLast(string streamId, string[] eventFilters, int? version = null)
        {

            using var context = GetDbContext(_sQLServerSnapshotStoreOptions);

            var eventFilter = string.Concat(eventFilters);

            var aggregateSnapshotQueryable = context.GetAggregateDbSet().AsQueryable().OrderByDescending(p => p.LastModifiedUtc);

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

            var aggregate = aggregateSnapshot.SerializedAggregate.JsonTo<TAggregate>();

            return aggregate;

        }

        public async Task<TAggregate[]> GetByVersionOrLast(string[] eventFilters, int? version = null)
        {
            using var context = GetDbContext(_sQLServerSnapshotStoreOptions);

            var eventFilter = string.Concat(eventFilters);

            var isLatest = version == null;

            AggregateSnapshot[] aggregateSnapshots = null;

            if (isLatest)
            {

                var orderByDescendingQueryable = context.GetAggregateDbSet().AsQueryable().OrderByDescending(snapshot => snapshot.LastModifiedUtc);

                //https://github.com/dotnet/efcore/issues/13805
                aggregateSnapshots = await context.GetAggregateDbSet().AsQueryable()
                                                                    .Where(snapshot => snapshot.EventFilter == eventFilter)
                                                                    .OrderByDescending(snapshot => snapshot.LastModifiedUtc)
                                                                    .Select(snapshot => snapshot.StreamId)
                                                                    .Distinct()
                                                                    .SelectMany(snapshot => orderByDescendingQueryable.Where(b => b.StreamId == snapshot).Take(1), (streamId, aggregateSnapshot) => aggregateSnapshot)
                                                                    .ToArrayAsync();
            }
            else
            {
                aggregateSnapshots = await context.GetAggregateDbSet().AsQueryable().Where(snapshot => snapshot.EventFilter == eventFilter && snapshot.Version == version).ToArrayAsync();
            }

            if (aggregateSnapshots.Length == 0) return new TAggregate[0];

            return aggregateSnapshots.Select(aggregateSnapshot => aggregateSnapshot.SerializedAggregate.JsonTo<TAggregate>()).ToArray();

        }

        public async Task Save(string[] eventFilters, TAggregate aggregate)
        {
            using var context = GetDbContext(_sQLServerSnapshotStoreOptions);

            var aggregateSnapshot = new TAggregateSnapshot
            {
                StreamId = aggregate.EntityId,
                Version = aggregate.Version,
                EventFilter = string.Concat(eventFilters),
                SerializedAggregate = aggregate.ToJson(),
            };

            context.GetAggregateDbSet().Add(aggregateSnapshot);

            await context.SaveChangesAsync();

        }

        public async Task<TAggregate[]> GetAll()
        {
            var results = new List<TAggregate>();

            using var context = GetDbContext(_sQLServerSnapshotStoreOptions);

            foreach (var aggregateSnapshot in await context.GetAggregateDbSet().AsQueryable().ToListAsync())
            {
                var aggregate = aggregateSnapshot.SerializedAggregate.JsonTo<TAggregate>();

                results.Add(aggregate);
            }

            return results.ToArray();

        }
    }
}
