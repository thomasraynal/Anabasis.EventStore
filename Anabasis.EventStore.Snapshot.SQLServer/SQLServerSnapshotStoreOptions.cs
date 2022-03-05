using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Snapshot.SQLServer
{
    public class SQLServerSnapshotStoreOptions : BaseConfiguration
    {
        [Required]
        public string ConnectionString { get; set; }
    }
}
