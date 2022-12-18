using Anabasis.Common;
using EventStore.ClientAPI;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Shared
{
    public class EventStorePersistentSubscriptionMessage : BaseEventStoreMessage
    {
        private readonly EventStorePersistentSubscriptionBase? _eventStorePersistentSubscriptionBase;

        public EventStorePersistentSubscriptionMessage(Guid messageId, IEvent content, ResolvedEvent resolvedEvent, EventStorePersistentSubscriptionBase? eventStorePersistentSubscriptionBase, Guid? traceId = null) : base(messageId, content, resolvedEvent, traceId)
        {
            _eventStorePersistentSubscriptionBase = eventStorePersistentSubscriptionBase;
        }

        protected override Task AcknowledgeInternal()
        {
            _eventStorePersistentSubscriptionBase?.Acknowledge(ResolvedEvent);

            return Task.CompletedTask;
        }

        protected override Task NotAcknowledgeInternal(string? reason = null)
        {
            _eventStorePersistentSubscriptionBase?.Fail(ResolvedEvent, PersistentSubscriptionNakEventAction.Retry, reason);

            return Task.CompletedTask;
        }
    }
}
