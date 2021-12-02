using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing.Domain
{
    public class DesactivateCurrencyPairCommandResult : CommandResult
    {
        public string Market { get; set; }
    }
}
