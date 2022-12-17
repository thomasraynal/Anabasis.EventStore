using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public interface IQueueBuffer : IDisposable
    {
        bool HasMessages { get; }
        bool CanPush { get; }
        bool CanPull { get; }
        IMessage[] Pull(int? maxNumberOfMessage = null);
        bool TryPull(out IMessage[] pulledMessages, int? maxNumberOfMessage = null);
        IMessage[] TryPush(IMessage[] messages, out IMessage[] unProcessedMessages);
        void Push(IMessage message);
        Task<IMessage[]> Flush(bool nackMessages);

    }
}
