using System.Collections.Generic;


namespace Anabasis.EventStore
{

    public abstract class BaseAggregate<TKey> : IAggregate<TKey>
    {
        private readonly List<IEvent<TKey>> _pendingEvents = new List<IEvent<TKey>>();
        private readonly List<IEvent<TKey>> _appliedEvents = new List<IEvent<TKey>>();

        public TKey EntityId { get; set; }

        public int Version { get; private set; } = -1;

        public void Mutate<TEntity>(IMutable<TKey, TEntity> @event) where TEntity : class, IAggregate<TKey>
        {
            @event.Apply(this as TEntity);
            Version++;
        }

        public void ApplyEvent(IEvent<TKey> @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = true)
        {
            //we only save applied events
            if (keepAppliedEventsOnAggregate && !saveAsPendingEvent)
            {
                _appliedEvents.Add(@event);
            }

            if (saveAsPendingEvent)
            {
                _pendingEvents.Add(@event);
                @event.EntityId = EntityId;
                return;
            }

            ((dynamic)this).Mutate((dynamic)@event);

        }

        public ICollection<IEvent<TKey>> GetPendingEvents()
        {
            return _pendingEvents;
        }

        public void ClearPendingEvents()
        {
            _pendingEvents.Clear();
        }

        public virtual string GetStreamName()
        {
            return EntityId.ToString();
        }

        public ICollection<IEvent<TKey>> GetAppliedEvents()
        {
            return _appliedEvents;
        }
    }
}
