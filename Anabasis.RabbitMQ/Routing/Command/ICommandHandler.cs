using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface ICommandHandler
    {
        void Handle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult;

        void UnHandle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult;
    }
}
