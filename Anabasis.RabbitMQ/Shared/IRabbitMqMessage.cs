using Anabasis.Common;
using System;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqMessage : IEvent
    {
        string Subject { get; }
    }
}
