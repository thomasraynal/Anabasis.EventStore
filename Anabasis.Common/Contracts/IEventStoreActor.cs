using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IEventStoreActor : IActor
    {
        Task EmitEventStore<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent;
        Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse;
        void SubscribeToEventStream(IEventStoreStream eventStoreStream, bool closeUnderlyingSubscriptionOnDispose = false);
    }
}
