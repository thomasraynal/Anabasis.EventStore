using Anabasis.EventStore.Event;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anabasis.Common;

namespace Anabasis.EventStore.Actor
{
    public interface IStatelessActor
    {
        string Id { get; }
        bool IsConnected { get; }
        IEventStoreStream[] Streams { get; }
        Task WaitUntilConnected(TimeSpan? timeout = null);
        public Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent;
        Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse;
        void SubscribeTo(IEventStoreStream eventStoreStream, bool closeSubscriptionOnDispose = false);
    }
}
