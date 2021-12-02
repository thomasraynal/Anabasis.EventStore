using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RabbitMQPlayground.Routing.Domain
{
    public class TraderConfiguration : ITraderConfiguration
    {
        public TraderConfiguration(string eventExchange, Expression<Func<PriceChangedEvent, bool>> routingStrategy)
        {
            EventExchange = eventExchange;
            RoutingStrategy = routingStrategy;
        }

        public string EventExchange { get; private set; }
        public Expression<Func<PriceChangedEvent, bool>> RoutingStrategy { get; private set; }
    }
}
