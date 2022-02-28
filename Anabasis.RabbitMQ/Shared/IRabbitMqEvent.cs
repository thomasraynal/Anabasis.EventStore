using Anabasis.Common;
using System;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqEvent : IEvent
    {
        Guid MessageId { get; }
        string Subject { get; }
    }
}
