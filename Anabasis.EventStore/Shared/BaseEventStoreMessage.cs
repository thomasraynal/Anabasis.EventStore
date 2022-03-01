using Anabasis.Common;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Shared
{
    public abstract class BaseEventStoreMessage : IEventStoreMessage
    {
        public BaseEventStoreMessage(Guid? messageId,IEvent content, ResolvedEvent resolvedEvent)
        {
            MessageId = messageId ?? Guid.NewGuid();
            Content = content;
            ResolvedEvent = resolvedEvent;
        }

        public Guid MessageId { get; }

        public IEvent Content { get; }

        public ResolvedEvent ResolvedEvent { get; }

        public abstract Task Acknowledge();

        public abstract Task NotAcknowledge(string reason = null);
    }
}
