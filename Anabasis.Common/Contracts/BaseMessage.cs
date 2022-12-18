using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Contracts
{
    public abstract class BaseMessage : IMessage
    {

        private readonly Subject<bool> _onAcknowledgeSubject;

        public BaseMessage(Guid messageId, IEvent content, Guid? traceId = null)
        {
            MessageId = messageId;
            Content = content;
            TraceId = traceId;

            _onAcknowledgeSubject = new Subject<bool>();

        }

        public Guid? TraceId { get; }

        public Guid MessageId { get;  }

        public bool IsAcknowledged { get; private set; }

        public IObservable<bool> OnAcknowledged => _onAcknowledgeSubject;

        public IEvent Content { get; }

        protected abstract Task AcknowledgeInternal();

        public async Task Acknowledge()
        {
           await AcknowledgeInternal();

            IsAcknowledged = true;

            _onAcknowledgeSubject.OnNext(true);
            _onAcknowledgeSubject.OnCompleted();
        }

        protected abstract Task NotAcknowledgeInternal(string? reason = null);

        public async Task NotAcknowledge(string? reason = null)
        {
            await NotAcknowledgeInternal();

            IsAcknowledged = false;

            _onAcknowledgeSubject.OnNext(false);
            _onAcknowledgeSubject.OnCompleted();
        }
    }
}
