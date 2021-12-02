using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface ICommandErrorResult : ICommandResult
    {
        int ErrorCode { get; set; }
        string ErrorMessage { get; set; }
    }
}
