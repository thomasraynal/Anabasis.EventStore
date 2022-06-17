using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeezUP2.Framework.EventHubs
{
    public static class EventHubsConstants
    {
        public const string EVENTHUB_CONSUMERS_SETTINGS_DEFAULT_KEY = "Default";

        public const string EventIdNameInEventProperty = "BeezUPEventId";
        public const string EventTypeNameInEventProperty = "BeezUPEventType";
        public const string IsZippedInEventProperty = "BeezUPIsZipped";
        public const string BlobUriInEventProperty = "BeezUPBlobUri";

        public const string Default_MonitoringTableName = "HubsProcessorCheckpoints";

        public const int Default_MaxInProgressEventdataCount = 300;
    }
}
