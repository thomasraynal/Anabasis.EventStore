using Anabasis.RabbitMQ;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{

    public interface IEventSubscription
    {
        string SubscriptionId { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        Func<IEvent, Task> OnEvent { get; }
    }
}
