using Anabasis.Common;
using Anabasis.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Anabasis.EventStore.Snapshot.SQLServer
{
    public abstract class BaseAggregateSnapshotDbContextFactory<TAggregateDbContext> : IDesignTimeDbContextFactory<TAggregateDbContext>
        where TAggregateDbContext : DbContext
    {
        protected abstract TAggregateDbContext CreateDbContextInternal(SqlServerSnapshotStoreOptions sQLServerSnapshotStoreOptions);

        public TAggregateDbContext CreateDbContext(string[] _)
        {

            var anabasisConfiguration = Configuration.GetConfigurations();

            var sqlServerSnapshotStoreOptions = anabasisConfiguration.ConfigurationRoot
                                                                     .GetSection(nameof(SqlServerSnapshotStoreOptions))
                                                                     .Get<SqlServerSnapshotStoreOptions>();

            return CreateDbContextInternal(sqlServerSnapshotStoreOptions);

        }
    }
}
