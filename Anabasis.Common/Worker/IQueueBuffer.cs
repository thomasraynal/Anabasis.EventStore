using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public interface IQueueBuffer : IDisposable
    {
        bool CanPush { get; }
        bool CanDequeue();
        IMessage[] Pull(int? maxNumberOfMessage = null);
        void TryPush(IMessage[] messages, out IMessage[] unProcessedMessages);
        void Push(IMessage message);
        Task Flush(bool nackMessages);
    }
}
