using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Queue;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.Actor
{
  public interface IActor
  {
    string Id { get; }
    Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders);
    Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse;
    void SubscribeTo(IEventStoreQueue eventStoreQueue, bool closeSubscriptionOnDispose = false);
  }
}
