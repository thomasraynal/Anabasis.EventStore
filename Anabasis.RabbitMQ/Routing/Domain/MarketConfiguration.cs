using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class MarketConfiguration : IMarketConfiguration
    {
        public MarketConfiguration(string name, string eventExchange)
        {
            Name = name;
            EventExchange = eventExchange;
        }

        public string Name { get; private set; }
        public string EventExchange { get; private set; }
    }
}
