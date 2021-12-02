using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class CommandErrorResult : ICommandErrorResult
    {
        public CommandErrorResult()
        {
            IsError = true;
        }

        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsError { get; set; }
    }
}
