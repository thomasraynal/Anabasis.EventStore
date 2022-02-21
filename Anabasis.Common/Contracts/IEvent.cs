using System;

namespace Anabasis.Common
{
    public interface IEvent: IHaveEntityId
    {
        string Name { get; }
        Guid EventID { get; }
        Guid CorrelationID { get; }
        bool IsCommand { get; }
        DateTime Timestamp { get; }
    }
}
