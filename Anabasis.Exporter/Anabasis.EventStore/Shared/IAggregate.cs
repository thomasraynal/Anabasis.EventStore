using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public interface IAggregate<TKey> : IEventStoreEntity<TKey>
  {
    int Version { get; }
    void ApplyEvent(IEvent<TKey> @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = false);
    IEvent<TKey>[] PendingEvents { get; }
    IEvent<TKey>[] AppliedEvents { get; }
    void ClearPendingEvents();
  }
}
