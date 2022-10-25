using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Demo
{
    public class EventCreated : BaseEvent
    {
        public EventCreated(string entityId, Guid? correlationId = null, Guid? causeId = null, Guid? traceId = null) : base(entityId, correlationId, causeId, traceId)
        {
        }
    }
}
