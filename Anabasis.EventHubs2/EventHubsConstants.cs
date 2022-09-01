using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    public static class EventHubsConstants
    {
        public const string EVENTHUB_CONSUMERS_SETTINGS_DEFAULT_KEY = "Default";

        public const string EventIdNameInEventProperty = "AnabasisEventId";
        public const string EventTypeNameInEventProperty = "AnabasisEventType";
        public const string IsZippedInEventProperty = "AnabasisIsZipped";
        public const string BlobUriInEventProperty = "AnabasisBlobUri";

        public const string Default_MonitoringTableName = "HubsProcessorCheckpoints";

        public const int Default_MaxInProgressEventdataCount = 300;
    }
}
