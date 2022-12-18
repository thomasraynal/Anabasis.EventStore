using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Contracts
{
    public interface IAcknowledgable
    {
        bool IsAcknowledged { get; }
        IObservable<bool> OnAcknowledged { get; }
        Task Acknowledge();
        Task NotAcknowledge(string? reason = null);
    }
}
