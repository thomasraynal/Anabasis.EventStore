using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public interface IQueueBuffer : IAsyncDisposable
    {
        bool CanAdd { get; }
        bool CanDequeue();
        IMessage[] Pull();
        Task Clear();
    }
}
