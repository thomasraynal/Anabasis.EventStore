using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Routing.Bus
{
    public interface IRabbitMqEventHandler
    {
        public bool CanHandle(Type eventType);
        public Task Handle(IRabbitMqMessage rabbitMqEvent);
    }
}
