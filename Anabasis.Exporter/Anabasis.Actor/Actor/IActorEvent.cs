using Anabasis.EventStore;
using System;

namespace Anabasis.Actor
{
  public interface IActorEvent : IEvent<string>
  {
    Guid EventID { get; }
    Guid CorrelationID { get; }
    string Log();
  }
}
