using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IMessage
    {
        Guid? TraceId { get; }
        Guid MessageId { get; }
        bool IsAcknowledged { get; }
        Task Acknowledge();
        Task NotAcknowledge(string? reason = null);
        IEvent Content { get; } 
    }
}
