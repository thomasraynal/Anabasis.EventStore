using Anabasis.Common.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IActor
    {
        string Id { get; }
        bool IsConnected { get; }
        TBus GetConnectedBus<TBus>();
        Task WaitUntilConnected(TimeSpan? timeout = null);
        Task EmitEventStore<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent;
        Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse;
        void SubscribeToEventStream(IEventStream eventStoreStream, bool closeUnderlyingSubscriptionOnDispose = false);
        void ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false);
    }
}
