using System;

namespace Anabasis.Common
{
    public interface IEvent: IHaveEntityId
    {
        Guid EventId { get; }
        Guid CorrelationId { get; }
        Guid? CauseId { get; }
        string Name { get; }
        bool IsCommand { get; }
        DateTime Timestamp { get; }
    }
}
