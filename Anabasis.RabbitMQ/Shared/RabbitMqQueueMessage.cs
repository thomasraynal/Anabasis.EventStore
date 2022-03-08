using Anabasis.Common;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqQueueMessage : IRabbitMqQueueMessage
    {
        private readonly IRabbitMqConnection _rabbitMqConnection;
        private readonly ulong _deliveryTag;
        private readonly bool _isAutoAck;

        public RabbitMqQueueMessage(
            Guid messageId,
            IRabbitMqConnection rabbitMqConnection, 
            Type type, 
            IRabbitMqEvent content, 
            bool redelivered, 
            ulong deliveryTag,
            bool isAutoAck)
        {
            _rabbitMqConnection = rabbitMqConnection;
            _deliveryTag = deliveryTag;
            _isAutoAck = isAutoAck;

            MessageId = messageId;
            Type = type;
            Content = content;
            DequeueCount = redelivered ? 1 : 0;
        }

        public Type Type { get; }
        public IEvent Content { get; }
        public int DequeueCount { get; }
        public Guid MessageId { get; }

        public Task Acknowledge()
        {
            if (!_isAutoAck)
            {
                _rabbitMqConnection.Acknowledge(_deliveryTag);
            }

            return Task.CompletedTask;
        }
        public Task NotAcknowledge(string reason = null)
        {
            if (!_isAutoAck)
            {
                _rabbitMqConnection.NotAcknowledge(_deliveryTag);
            }

            return Task.CompletedTask;
        }

    }
}
