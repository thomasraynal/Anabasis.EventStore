using System;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqEvent
    {
        Guid EventID { get; }
        Guid CorrelationID { get; }
        string Subject { get; }
    }
}
