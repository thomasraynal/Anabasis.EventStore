using System;

namespace Anabasis.Common
{
    public interface IEvent : IHaveAStreamId
    {
        Guid EventID { get; }
        Guid CorrelationID { get; }
        bool IsCommand { get; }
    }
}
