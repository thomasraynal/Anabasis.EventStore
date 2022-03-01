using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public interface IEventStoreBus: IBus
    {
        Task EmitEventStore<TEvent>(TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent;
        Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse;
        void SubscribeToEventStream(IEventStoreStream eventStoreStream, Action<IMessage,TimeSpan?> onMessageReceived, bool closeSubscriptionOnDispose = false);
    }
}