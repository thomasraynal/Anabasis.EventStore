using System;

namespace Anabasis.Common
{
    public interface IEvent
    {
        Guid EventID { get; }
        Guid CorrelationID { get; }
        bool IsCommand { get; }
        string EntityId { get; }
    }
}
