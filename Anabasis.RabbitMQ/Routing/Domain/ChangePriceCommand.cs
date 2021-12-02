using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing.Domain
{
    public class ChangePriceCommand : CommandBase
    {
        public ChangePriceCommand(string aggregateId, string target) : base(aggregateId, target)
        {
        }

        public double Ask { get; set; }
        public double Bid { get; set; }
        public string Counterparty { get; set; }
    }
}
