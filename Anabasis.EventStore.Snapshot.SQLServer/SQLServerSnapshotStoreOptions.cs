using Anabasis.Common;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.EventStore.Snapshot.SQLServer
{
    public class SqlServerSnapshotStoreOptions : BaseConfiguration
    {
#nullable disable
        [Required]
        public string ConnectionString { get; set; }
#nullable enable
    }
}
