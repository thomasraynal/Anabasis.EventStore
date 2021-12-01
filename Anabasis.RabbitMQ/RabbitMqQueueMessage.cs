using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqQueueMessage
    {
        private readonly RabbitMqConnection _rabbitMqConnection;
        private readonly ulong _deliveryTag;

        public RabbitMqQueueMessage(RabbitMqConnection rabbitMqConnection, IEvent content, bool redelivered, ulong deliveryTag)
        {
            _rabbitMqConnection = rabbitMqConnection;
            _deliveryTag = deliveryTag;

            Content = content;
            DequeueCount = redelivered ? 1 : 0;
        }

        public IEvent Content { get; private set; }
        public int DequeueCount { get; private set; }

        public void Acknowledge()
        {
            _rabbitMqConnection.DoWithChannel(channel => channel.BasicAck(_deliveryTag, multiple: false));
        }

        public void NotAcknowledge()
        {
            _rabbitMqConnection.DoWithChannel(channel => channel.BasicNack(_deliveryTag, multiple: false, requeue: true));
        }
    }
}
