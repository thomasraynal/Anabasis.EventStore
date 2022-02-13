using Anabasis.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;


namespace Anabasis.Common
{

    public abstract class BaseAggregate : IAggregate
    {
        private readonly List<IEntity> _pendingEvents = new();
        private readonly List<IEntity> _appliedEvents = new();

        [JsonProperty]
        public string EntityId { get; protected set; }

        public int Version { get; set; } = -1;

        [JsonProperty]
        public int VersionFromSnapshot { get; set; } = -1;

        public void Mutate<TEntity>(IMutation<TEntity> @event) where TEntity : class, IAggregate
        {
            @event.Apply(this as TEntity);
            Version++;
        }

        public void ApplyEvent(IEntity @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = true)
        {
            //we only save applied events
            if (keepAppliedEventsOnAggregate && !saveAsPendingEvent)
            {
                _appliedEvents.Add(@event);
            }

            if (saveAsPendingEvent)
            {
                _pendingEvents.Add(@event);
                return;
            }

            ((dynamic)this).Mutate((dynamic)@event);

        }

        public ICollection<IEntity> GetPendingEvents()
        {
            return _pendingEvents;
        }

        public void ClearPendingEvents()
        {
            _pendingEvents.Clear();
        }

        public void SetEntityId(string entityId)
        {
            if (null != EntityId)
                throw new InvalidOperationException($"EntityId is already set - current=[{EntityId}]  new=[{entityId}]");

            EntityId = entityId;
        }

        public IEntity[] PendingEvents
        {
            get
            {
                return _pendingEvents.ToArray();
            }
        }

        public IEntity[] AppliedEvents
        {
            get
            {
                return _appliedEvents.ToArray();
            }
        }

    }
}
