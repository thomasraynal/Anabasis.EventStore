using Anabasis.Common;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqQueueMessage : IRabbitMqQueueMessage
    {
        private readonly IRabbitMqConnection _rabbitMqConnection;
        private readonly ulong _deliveryTag;

        public RabbitMqQueueMessage(
            Guid messageId,
            IRabbitMqConnection rabbitMqConnection, 
            Type type, 
            IRabbitMqEvent content, 
            bool redelivered, 
            ulong deliveryTag)
        {
            _rabbitMqConnection = rabbitMqConnection;
            _deliveryTag = deliveryTag;

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
            _rabbitMqConnection.DoWithChannel(channel => channel.BasicAck(_deliveryTag, multiple: false));

            return Task.CompletedTask;
        }
        public Task NotAcknowledge()
        {
            _rabbitMqConnection.DoWithChannel(channel => channel.BasicNack(_deliveryTag, multiple: false, requeue: true));

            return Task.CompletedTask;
        }

    }
}
