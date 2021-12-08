using Anabasis.EventStore.Event;
using System;
using System.Threading.Tasks;
using Anabasis.Common;

namespace Anabasis.EventStore.Actor
{
    public interface IStatelessActor: IActor
    {
        IEventStream[] Streams { get; }
        Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse;
        void SubscribeTo(IEventStream eventStoreStream, bool closeSubscriptionOnDispose = false);
    }
}
