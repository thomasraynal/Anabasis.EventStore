using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class CommandResult : ICommandResult
    {
        public bool IsError { get; set; }
    }
}
