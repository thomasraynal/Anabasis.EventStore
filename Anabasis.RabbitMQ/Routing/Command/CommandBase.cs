using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class CommandBase : ICommand
    {
        public CommandBase(string aggregateId, string target)
        {
            AggregateId = aggregateId;
            Target = target;
        }

        public string AggregateId { get; }

        public string Target { get; }

        public Type EventType => this.GetType();
    }
}
