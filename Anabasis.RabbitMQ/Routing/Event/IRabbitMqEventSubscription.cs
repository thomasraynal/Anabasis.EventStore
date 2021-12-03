using Anabasis.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{

    public interface IRabbitMqEventSubscription
    {
        string SubscriptionId { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        Func<IRabbitMqEvent, Task> OnEvent { get; }
    }
}
