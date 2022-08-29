using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Worker
{
    public interface IQueueBuffer
    {
        bool CanAdd { get; }
        bool CanDequeue();
    }
}
