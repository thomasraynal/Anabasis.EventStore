using Anabasis.Common;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Shared
{
    public class EventStorePersistentSubscriptionMessage : BaseEventStoreMessage
    {
        private readonly EventStorePersistentSubscriptionBase _eventStorePersistentSubscriptionBase;

        public EventStorePersistentSubscriptionMessage(Guid? messageId, IEvent content, ResolvedEvent resolvedEvent, EventStorePersistentSubscriptionBase eventStorePersistentSubscriptionBase) : base(messageId, content, resolvedEvent)
        {
            _eventStorePersistentSubscriptionBase = eventStorePersistentSubscriptionBase;
        }

        public override Task Acknowledge()
        {
            _eventStorePersistentSubscriptionBase.Acknowledge(ResolvedEvent);

            return Task.CompletedTask;
        }

        public override Task NotAcknowledge(string reason = null)
        {
            _eventStorePersistentSubscriptionBase.Fail(ResolvedEvent, PersistentSubscriptionNakEventAction.Retry, reason);

            return Task.CompletedTask;
        }
    }
}
