using Anabasis.EventStore.Event;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Actor
{
    public interface IStatelessActor
    {
        string Id { get; }
        bool IsConnected { get; }
        IEventStoreQueue[] Queues { get; }
        Task WaitUntilConnected(TimeSpan? timeout = null);
        public Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent;
        Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse;
        void SubscribeTo(IEventStoreQueue eventStoreQueue, bool closeSubscriptionOnDispose = false);
    }
}
