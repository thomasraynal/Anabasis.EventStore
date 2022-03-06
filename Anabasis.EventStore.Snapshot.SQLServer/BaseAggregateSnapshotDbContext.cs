using Anabasis.Common;
using Anabasis.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot.SQLServer
{
    public abstract class BaseAggregateSnapshotDbContext<TAggregateSnapshot> : BaseAnabasisDbContext
        where TAggregateSnapshot: AggregateSnapshot
    {

        public BaseAggregateSnapshotDbContext(SQLServerSnapshotStoreOptions sQLServerSnapshotStoreOptions)
            : base(new DbContextOptionsBuilder().UseSqlServer(sQLServerSnapshotStoreOptions.ConnectionString).Options)
        {
        }

        protected BaseAggregateSnapshotDbContext(DbContextOptions options) : base(options)
        {
        }

        protected BaseAggregateSnapshotDbContext(DbContextOptionsBuilder dbContextOptionsBuilder) : base(dbContextOptionsBuilder)
        {
        }

        public abstract DbSet<TAggregateSnapshot> GetAggregateDbSet();

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

            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreatingInternal(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AggregateSnapshot>().HasKey(aggregateSnapshot => new
            {
                aggregateSnapshot.StreamId,
                aggregateSnapshot.EventFilter,
                aggregateSnapshot.Version
            });
        }
    }
}
