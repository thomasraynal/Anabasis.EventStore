using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    public class EventHubProcessorHostParameters : BaseConfiguration
    {
        public const int DEFAULT_MAX_BATCH_SIZE = 100;
        public static TimeSpan DEFAULT_LEASE_DURATION => TimeSpan.FromMinutes(1);
        public static TimeSpan DEFAULT_RENEW_INTERVAL => TimeSpan.FromSeconds(45);

        public EventHubProcessorHostParameters() { }

        public EventHubProcessorHostParameters(EventHubConnectionOptions eventHubConnectionOptions, 
            string eventProcessorHostName, 
            string consumerGroupName, 
            EventHubConsumerOptions eventHubConsumerSettings, 
            int maxBatchSize = DEFAULT_MAX_BATCH_SIZE)
        {
            EventHubConnectionOptions = eventHubConnectionOptions;
            EventProcessorHostName = eventProcessorHostName;
            ConsumerGroupName = consumerGroupName;
            EventHubConsumerOptions = eventHubConsumerSettings;
            MaxBatchSize = maxBatchSize;
        }

        public EventHubConnectionOptions EventHubConnectionOptions { get; set; }
        public string EventProcessorHostName { get; set; }
        public string ConsumerGroupName { get; set; }
        public EventHubConsumerOptions EventHubConsumerOptions { get; set; }
        public int MaxBatchSize { get; set; }

    }
}
