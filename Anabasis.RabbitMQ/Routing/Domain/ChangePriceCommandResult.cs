using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing.Domain
{
    public class ChangePriceCommandResult : CommandResult
    {
        public string Market { get; set; }
    }
}
