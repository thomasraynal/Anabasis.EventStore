using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class CurrencyPairDesactivated : EventBase
    {
        public CurrencyPairDesactivated(string aggregateId) : base(aggregateId)
        {
        }
    }
}