using Anabasis.Common;
using Anabasis.Common.Contracts;
using EventStore.ClientAPI;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Shared
{
    public abstract class BaseEventStoreMessage : BaseMessage, IEventStoreMessage
    {
        protected BaseEventStoreMessage(Guid messageId, IEvent content, ResolvedEvent resolvedEvent, Guid? traceId = null) : base(messageId, content, traceId)
        {
            ResolvedEvent = resolvedEvent;
        }

        public ResolvedEvent ResolvedEvent { get; }

    }
}
