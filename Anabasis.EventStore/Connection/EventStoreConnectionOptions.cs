using Anabasis.Common;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.EventStore.Connection
{
    public class EventStoreConnectionOptions : BaseConfiguration
    {
        public const int EVENTSTORE_DEFAULT_TCP_PORT = 1113;
        public const int EVENTSTORE_DEFAULT_HTTP_PORT = 2113;
        public const int EVENTSTORE_DEFAULT_HEARTBEAT_TIMEOUT= 1500;
        public const int EVENTSTORE_DEFAULT_OPERATION_TIMEOUT = 60000;

#nullable disable

        [Required]
        public string HostName { get; set; }
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }

#nullable enable

        public int TcpPortNumber { get; set; } = EVENTSTORE_DEFAULT_TCP_PORT;
        public int HttpPortNumber { get; set; } = EVENTSTORE_DEFAULT_HTTP_PORT;
        public bool DisableSslConnection { get; } = true;
        public bool DisableAuthentication { get; } = false;
        public bool ClusterEnabled { get; set; } = false;
        public int HeartBeatTimeout { get; set; } = EVENTSTORE_DEFAULT_HEARTBEAT_TIMEOUT;
        public bool VerboseLogging { get; set; } = false;
        public int OperationTimeout { get; set; } = EVENTSTORE_DEFAULT_OPERATION_TIMEOUT;

        public string ConnectionString
        {
            get
            {
                var userAndPassword = DisableAuthentication ? "" : $"{UserName}:{Password}@";
                var ssl = DisableSslConnection ? "UseSslConnection=false;" : "";

                var connectionString = 
                    $"ConnectTo=tcp://{userAndPassword}{HostName}:{TcpPortNumber}; " +
                    $"HeartBeatTimeout={HeartBeatTimeout}; " +
                    $"VerboseLogging={VerboseLogging.ToString().ToLowerInvariant()}; " +
                    $"OperationTimeout={OperationTimeout}; {ssl}";

                if (ClusterEnabled)
                {
                    connectionString = $"ClusterDns={HostName}; {connectionString}";
                }

                return connectionString;
            }
        }


    }
}
