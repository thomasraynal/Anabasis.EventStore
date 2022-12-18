using Anabasis.Common;
using Anabasis.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Shared
{
    public class EventHubMessage : BaseMessage, IEventHubMessage
    {
        public EventHubMessage(Guid messageId, IEvent content, Guid? traceId = null) : base(messageId, content, traceId)
        {
        }

        protected override Task AcknowledgeInternal()
        {
            return Task.CompletedTask;
        }

        protected override Task NotAcknowledgeInternal(string? reason = null)
        {
            return Task.CompletedTask;
        }
    }
}
