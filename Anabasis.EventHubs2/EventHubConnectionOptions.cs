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
    public class EventHubConnectionOptions : BaseConfiguration
    {

#nullable disable

        [DataMember]
        public string HubName { get; set; }

        [DataMember]
        public string Namespace { get; set; }

        [DataMember]
        public string SharedAccessKeyName { get; set; }

        [DataMember]
        public string SharedAccessKey { get; set; }

        [DataMember]
        public string Transport { get; set; } = "Amqp";

#nullable enable

        public override string ToString() => GetConnectionString();

        public string GetConnectionString() => $"Endpoint=sb://{Namespace}.servicebus.windows.net/;SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey};TransportType={Transport}";

 
    }
}
