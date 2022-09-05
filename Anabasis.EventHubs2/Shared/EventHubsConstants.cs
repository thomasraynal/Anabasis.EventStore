using System;

namespace Anabasis.EventHubs
{
    public static class EventHubsConstants
    {
        public const string EventIdNameInEventProperty = "AnabasisEventId";
        public const string MessageIdNameInEventProperty = "AnabasisMessageId";
        public const string EventTypeNameInEventProperty = "AnabasisEventType";
        public const string IsZippedInEventProperty = "AnabasisIsZipped";
        public const string DefaultMonitoringTableName = "HubsProcessorCheckpoints";
        public const string DefaultConsumerGroupName = "$Default";
        public static readonly TimeSpan DefaultCheckpointPeriod  = TimeSpan.FromSeconds(30);
        public const int DefaultMaxInProgressEventdataCount = 300;
    }
}
