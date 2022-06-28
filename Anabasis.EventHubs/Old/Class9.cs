using Microsoft.Azure.EventHubs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    class EventDataConsumable : GenericConsumable<EventData>
    {
        public EventDataConsumable(EventData content, Action<EventData> consumedAction) : base(content, consumedAction)
        {
        }
    }
}
