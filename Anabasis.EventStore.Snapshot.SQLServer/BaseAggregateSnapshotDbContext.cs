using Anabasis.Common;
using Anabasis.EntityFramework;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot.SQLServer
{
    public abstract class BaseAggregateSnapshotDbContext<TAggregateSnapshot> : BaseAnabasisDbContext
        where TAggregateSnapshot : class, IAggregateSnapshot
    {

        protected BaseAggregateSnapshotDbContext(DbContextOptions options) : base(options)
        {
        }

        protected BaseAggregateSnapshotDbContext(DbContextOptionsBuilder dbContextOptionsBuilder) : base(dbContextOptionsBuilder)
        {
        }

        public DbSet<TAggregateSnapshot> AggregateSnapshots { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {

            var now = DateTime.UtcNow;

            var entries = ChangeTracker
                .Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                if (entry.Entity is TAggregateSnapshot aggregateSnapshot)
                {
                    aggregateSnapshot.LastModifiedUtc = now;
                }
            }

            return base.SaveChangesAsync(cancellationToken);

        }

        protected override void OnModelCreatingInternal(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TAggregateSnapshot>().HasKey(aggregateSnapshot => new
            {
                aggregateSnapshot.EntityId,
                aggregateSnapshot.EventFilter,
                aggregateSnapshot.Version
            });
        }
    }
}
