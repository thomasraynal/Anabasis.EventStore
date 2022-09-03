using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    [DataContract, Serializable]
    public class EventHubConsumerCheckpointSettings : BaseConfiguration
    {
#nullable disable

        [DataMember]
        public string CheckpointStoreAccountName { get; set; }
        [DataMember]
        public string CheckpointStoreAccountKey { get; set; }
        [DataMember]
        public bool UseHttps { get; set; } = true;
        public string GetConnectionString() => $"DefaultEndpointsProtocol=https;AccountName={CheckpointStoreAccountName};AccountKey={CheckpointStoreAccountKey};EndpointSuffix=core.windows.net;UseHttps={UseHttps}";
        [DataMember]
        public string BlobContainerName { get; set; }

#nullable enable

    }

    [DataContract, Serializable]
    public class EventHubOptions : BaseConfiguration
    {

#nullable disable

        [DataMember]
        public string MaximumBatchSize { get; set; }

        [DataMember]
        public string HubName { get; set; }

        [DataMember]
        public string ConsumerGroup { get; set; }

        [DataMember]
        public string Namespace { get; set; }

        [DataMember]
        public string SharedAccessKeyName { get; set; }

        [DataMember]
        public string SharedAccessKey { get; set; }

        [DataMember]
        public EventHubConsumerCheckpointSettings EventHubConsumerSettings { get; set; }

#nullable enable

        public string GetConnectionString() => $"Endpoint=sb://{Namespace}.servicebus.windows.net/;SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";

    }
}
