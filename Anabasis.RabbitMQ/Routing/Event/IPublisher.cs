using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{
    public interface IPublisher
    {
        void Emit(IEvent @event, string exchange);
        Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : ICommandResult;
    }
}
