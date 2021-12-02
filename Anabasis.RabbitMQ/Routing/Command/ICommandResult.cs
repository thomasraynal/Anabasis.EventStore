using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface ICommandResult
    {
        bool IsError { get; set; }
    }
}
