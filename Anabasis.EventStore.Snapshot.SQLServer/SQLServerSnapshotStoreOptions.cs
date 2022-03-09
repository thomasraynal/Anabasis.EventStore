using Anabasis.Common;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.EventStore.Snapshot.SQLServer
{
    public class SqlServerSnapshotStoreOptions : BaseConfiguration
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
