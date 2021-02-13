using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public interface IAggregate<TKey>: IEventStoreEntity<TKey>
    {
        int Version { get; }
        void ApplyEvent(IEvent<TKey> @event, bool saveAsPendingEvent = true, bool saveEvent = false);
        ICollection<IEvent<TKey>> GetAppliedEvents();
        ICollection<IEvent<TKey>> GetPendingEvents();
        void ClearPendingEvents();
    }
}
