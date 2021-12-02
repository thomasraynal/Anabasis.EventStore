using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class DesactivateCurrencyPairCommand : CommandBase
    {
        public DesactivateCurrencyPairCommand(string aggregateId, string target) : base(aggregateId, target)
        {
        }
    }
}
