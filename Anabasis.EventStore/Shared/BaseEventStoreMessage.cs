using Anabasis.Common;
using EventStore.ClientAPI;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Shared
{
    public abstract class BaseEventStoreMessage : IEventStoreMessage
    {
        public BaseEventStoreMessage(Guid? messageId, IEvent content, ResolvedEvent resolvedEvent, Guid? traceId = null)
        {
            MessageId = messageId ?? Guid.NewGuid();
            Content = content;
            ResolvedEvent = resolvedEvent;
            TraceId = traceId;
        }

        public Guid MessageId { get; }

        public IEvent Content { get; }

        public ResolvedEvent ResolvedEvent { get; }

        public Guid? TraceId { get; }

        public bool IsAcknowledged { get; internal set; }

        public IObservable<bool> OnAcknowledged => throw new NotImplementedException();

        public abstract Task Acknowledge();

        public abstract Task NotAcknowledge(string? reason = null);
    }
}
