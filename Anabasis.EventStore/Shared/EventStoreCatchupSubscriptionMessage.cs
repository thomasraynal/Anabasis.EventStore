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
        public EventStoreCatchupSubscriptionMessage(Guid? messageId, IEvent content, ResolvedEvent resolvedEvent) : base(messageId, content, resolvedEvent)
        {
        }

        public override Task Acknowledge()
        {
            return Task.CompletedTask;
        }

        public override Task NotAcknowledge(string reason = null)
        {
            return Task.CompletedTask;
        }
    }
}
