using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Contracts
{
    public abstract class BaseMessage : IMessage
    {

        public BaseMessage(Guid messageId, IEvent content, Guid? traceId = null)
        {
            MessageId = messageId;
            Content = content;
            TraceId = traceId;

        }

        public Guid? TraceId { get; }

        public Guid MessageId { get;  }

        public bool IsAcknowledged { get; private set; }

        public abstract IObservable<bool> OnAcknowledged { get; }

        public IEvent Content { get; }

        protected abstract Task AcknowledgeInternal();

        public Task Acknowledge()
        {
            throw new NotImplementedException();
        }

        protected abstract Task NotAcknowledgeInternal();

        public Task NotAcknowledge(string? reason = null)
        {
            throw new NotImplementedException();
        }
    }
}
