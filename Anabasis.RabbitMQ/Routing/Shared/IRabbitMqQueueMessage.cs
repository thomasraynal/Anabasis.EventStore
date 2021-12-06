using System;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqQueueMessage
    {
        IRabbitMqEvent Content { get; }
        int DequeueCount { get; }
        Type Type { get; }
        void Acknowledge();
        void NotAcknowledge();
    }
}