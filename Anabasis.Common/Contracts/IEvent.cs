using System;

namespace Anabasis.Common
{
    public interface IEvent: IHaveEntityId
    {
        Guid EventID { get; }
        Guid CorrelationID { get; }
        bool IsCommand { get; }
    }
}
