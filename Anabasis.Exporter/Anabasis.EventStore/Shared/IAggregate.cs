using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public interface IAggregate<TKey> : IEntityEvent<TKey>
  {
    int Version { get; }
    void ApplyEvent(IEntityEvent<TKey> @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = false);
    IEntityEvent<TKey>[] PendingEvents { get; }
    IEntityEvent<TKey>[] AppliedEvents { get; }
    void ClearPendingEvents();
  }
}
