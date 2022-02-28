using Anabasis.Common;
using System;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqQueueMessage: IMessage
    {
        Type Type { get; }
    }
}