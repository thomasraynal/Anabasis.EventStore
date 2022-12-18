using Anabasis.Common;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Shared
{
    public class EventStoreCatchupSubscriptionMessage : BaseEventStoreMessage
    {
        public EventStoreCatchupSubscriptionMessage(Guid messageId, IEvent content, ResolvedEvent resolvedEvent, Guid? traceId = null) : base(messageId, content, resolvedEvent, traceId)
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
