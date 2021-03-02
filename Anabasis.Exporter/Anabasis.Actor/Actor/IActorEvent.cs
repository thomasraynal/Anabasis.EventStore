using Anabasis.EventStore;
using System;

namespace Anabasis.Actor
{
  public interface IActorEvent : IEntityEvent<string>
  {
    Guid EventID { get; }
    Guid CorrelationID { get; }
    string Log();
  }
}
