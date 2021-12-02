using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQPlayground.Routing.Event;

namespace RabbitMQPlayground.Routing
{
    public abstract class CommandSubscriptionBase : ICommandSubscription
    {
        protected CommandSubscriptionBase(string queueName)
        {
            Target = queueName;
        }

        public abstract string  SubscriptionId { get; }

        public string Target { get; }

        public Func<ICommand, ICommandResult> OnCommand { get; protected set; }
    }
}
