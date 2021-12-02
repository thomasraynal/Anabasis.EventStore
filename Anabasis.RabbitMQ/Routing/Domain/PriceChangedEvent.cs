using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing.Domain
{
    public class PriceChangedEvent : EventBase
    {
        public PriceChangedEvent(string aggregateId) : base(aggregateId)
        {
        }

        public double Ask { get; set; }
        public double Bid { get; set; }

        [RoutingPosition(1)]
        public string Counterparty { get; set; }

    }
}
