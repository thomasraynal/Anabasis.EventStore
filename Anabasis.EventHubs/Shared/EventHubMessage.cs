using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Shared
{
    public class EventHubMessage : IEventHubMessage
    {
        public EventHubMessage(Guid messageId, IEvent content, Guid? traceId = null)
        {
            MessageId = messageId;
            Content = content;
            TraceId = traceId;

        }

        public Guid MessageId { get; }

        public IEvent Content { get; }

        public bool IsAcknowledged { get; private set; }

        public Guid? TraceId { get; }

        public IObservable<bool> OnAcknowledged => throw new NotImplementedException();

        public Task Acknowledge()
        {
            IsAcknowledged = true;

            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string? reason = null)
        {
            IsAcknowledged = false;

            return Task.CompletedTask;
        }
    }
}
