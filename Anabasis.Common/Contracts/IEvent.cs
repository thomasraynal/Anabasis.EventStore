using System;

namespace Anabasis.Common
{
    public interface IEvent: IHaveEntityId
    {
        Guid? TraceId { get; }
        Guid EventId { get; }
        Guid CorrelationId { get; }
        Guid? CauseId { get; }
        string EventName { get; }
        bool IsCommand { get; }
        DateTime Timestamp { get; }
        bool IsAggregateEvent { get; }

    }
}
