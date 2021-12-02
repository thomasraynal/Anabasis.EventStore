using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    [Serializable]
    public class CommandFailureException : Exception
    {
        public ICommandErrorResult Error { get; private set; }

        public CommandFailureException()
        {
        }

        public CommandFailureException(ICommandErrorResult error)
        {
            Error = error;
        }

        public CommandFailureException(string message) : base(message)
        {
        }

        public CommandFailureException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected CommandFailureException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
