using System;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqQueueMessage
    {
        IRabbitMqMessage Content { get; }
        int DequeueCount { get; }
        Type Type { get; }
        void Acknowledge();
        void NotAcknowledge();
    }
}