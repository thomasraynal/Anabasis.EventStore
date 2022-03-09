using Anabasis.Common;
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
            var configurationBuilder = new ConfigurationBuilder();

            configurationBuilder.AddJsonFile(AnabasisAppContext.AppConfigurationFile, false, false);
            configurationBuilder.AddJsonFile(AnabasisAppContext.GroupConfigurationFile, true, false);

            var configurationRoot = configurationBuilder.Build();

            var appConfigurationOptions = new AppConfigurationOptions();
            configurationRoot.GetSection(nameof(AppConfigurationOptions)).Bind(appConfigurationOptions);

            appConfigurationOptions.Validate();

            var groupConfigurationOptions = new GroupConfigurationOptions();
            configurationRoot.GetSection(nameof(GroupConfigurationOptions)).Bind(groupConfigurationOptions);

            var sqlServerSnapshotStoreOptions = configurationRoot.GetSection(nameof(SqlServerSnapshotStoreOptions))
                                                                 .Get<SqlServerSnapshotStoreOptions>();

            return CreateDbContextInternal(sqlServerSnapshotStoreOptions);

        }
    }
}
