using System;

namespace Anabasis.RabbitMQ
{
    public interface IEvent
    {
        Guid EventID { get; }
        Guid CorrelationID { get; }
        string Subject { get; }
    }
}
