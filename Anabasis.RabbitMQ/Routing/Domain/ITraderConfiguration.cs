using System;
using System.Linq.Expressions;

namespace RabbitMQPlayground.Routing.Domain
{
    public interface ITraderConfiguration
    {
        string EventExchange { get; }
        Expression<Func<PriceChangedEvent, bool>> RoutingStrategy { get; }
    }
}