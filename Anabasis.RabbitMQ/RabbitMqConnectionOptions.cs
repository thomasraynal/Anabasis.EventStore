﻿using Anabasis.Api.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Text;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqConnectionOptions : BaseConfiguration
    {
        public const int RABBIT_MQ_DEFAULT_PORT = 5672;
        public const int RABBIT_MQ_DEFAULT_MANAGER_PORT = 15672;
        public const int DEFAULT_RABBITMQ_PREFETCH_COUNT = 100;

        [Required]
        public string HostName { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }

        public ushort PrefetchCount { get; set; } = DEFAULT_RABBITMQ_PREFETCH_COUNT;

        public int Port { get; set; } = RABBIT_MQ_DEFAULT_PORT;

        public int ManagerPort { get; set; } = RABBIT_MQ_DEFAULT_MANAGER_PORT;


    }
}
