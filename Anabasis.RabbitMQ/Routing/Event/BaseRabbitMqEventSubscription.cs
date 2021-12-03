using Anabasis.RabbitMQ;
using RabbitMQPlayground.Routing.Shared;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{
    public abstract class BaseRabbitMqEventSubscription : IRabbitMqEventSubscription
    {
        public BaseRabbitMqEventSubscription(string exchange, Expression<Func<IRabbitMqEvent, bool>> routingStrategy)
        {
            var rabbitMQSubjectExpressionVisitor = new RabbitMQSubjectExpressionVisitor(GetType());

            rabbitMQSubjectExpressionVisitor.Visit(routingStrategy);

            Exchange = exchange;
            RoutingKey = rabbitMQSubjectExpressionVisitor.Resolve();
        }

        public string Exchange { get; }
        public string RoutingKey { get; }
        public abstract string SubscriptionId { get; }
        public Func<IRabbitMqEvent, Task> OnEvent { get; }
    }
}
