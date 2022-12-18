using Anabasis.Common.Contracts;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IMessage: IAcknowledgable
    {
        Guid? TraceId { get; }
        Guid MessageId { get; }
        IEvent Content { get; } 
    }
}
