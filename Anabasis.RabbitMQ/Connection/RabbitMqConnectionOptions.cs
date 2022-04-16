using Anabasis.Common;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqConnectionOptions : BaseConfiguration
    {
        public const int RABBIT_MQ_DEFAULT_PORT = 5672;
        public const int RABBIT_MQ_DEFAULT_MANAGER_PORT = 15672;
        public const int DEFAULT_RABBITMQ_PREFETCH_COUNT = 0;
        public const int DEFAULT_RABBITMQ_PREFETCH_SIZE = 0;

#nullable disable

        [Required]
        public string HostName { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

#nullable enable


        public ushort PrefetchCount { get; set; } = DEFAULT_RABBITMQ_PREFETCH_COUNT;
        public ushort PrefetchSize { get; set; } = DEFAULT_RABBITMQ_PREFETCH_SIZE;
        public int Port { get; set; } = RABBIT_MQ_DEFAULT_PORT;
        public int ManagerPort { get; set; } = RABBIT_MQ_DEFAULT_MANAGER_PORT;
        public bool DoAppCrashOnFailure { get; set; } = false;
        public bool AutoCreateExchange { get; set; } = true;
    }
}
