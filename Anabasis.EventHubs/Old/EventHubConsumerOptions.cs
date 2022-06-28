using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    [DataContract]
    [Serializable]
    public class EventHubConsumerOptions : BaseConfiguration
    {
        public AzureStorageConnectionOptions BlobStorage { get; set; }
        public AzureStorageConnectionOptions TableStorage { get; set; }

        public int? LeaseDurationInSec { get; set; } = null;
        public int? RenewIntervalInSec { get; set; } = null;

    }
}
