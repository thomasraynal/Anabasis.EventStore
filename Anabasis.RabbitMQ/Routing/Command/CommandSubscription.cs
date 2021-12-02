using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class CommandSubscription<TCommand, TCommandResult> : CommandSubscriptionBase, ICommandSubscription<TCommand, TCommandResult> where TCommand : class, ICommand
        where TCommandResult : ICommandResult
    {
        public CommandSubscription(string queueName, Func<TCommand, TCommandResult> onCommand) : base(queueName)
        {
            OnTypedCommand = onCommand;

            OnCommand = (command) =>
            {
                return OnTypedCommand(command as TCommand);
            };
        }

        public Func<TCommand, TCommandResult> OnTypedCommand { get; }

        public override string SubscriptionId => $"{Target}.{typeof(TCommand)}";

        public override bool Equals(object obj)
        {
            return obj is CommandSubscription<TCommand, TCommandResult> subscription &&
                   SubscriptionId.Equals(subscription.SubscriptionId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SubscriptionId);
        }

    }
}
