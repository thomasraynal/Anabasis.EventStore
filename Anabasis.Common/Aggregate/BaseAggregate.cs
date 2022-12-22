using Anabasis.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Anabasis.Common
{
    public abstract class BaseAggregate : IAggregate
    {
        private readonly List<IEvent> _pendingEvents = new();
        private readonly List<IEvent> _appliedEvents = new();


        [JsonProperty]
        public string? EntityId { get; protected set; }

        public long Version { get; set; } = -1;

        [JsonProperty]
        public long VersionFromSnapshot { get; set; } = -1;

        public void ApplyEvent<TAggregate>(
            IAggregateEvent<TAggregate> @event, 
            bool saveAsPendingEvent = true, 
            bool keepAppliedEventsOnAggregate = true) 
            where TAggregate : class, IAggregate
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

#nullable disable

            @event.Apply(this as TAggregate);

#nullable enable

            Version = @event.EventNumber;
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

        public IEvent[] PendingEvents
        {
            get
            {
                return _pendingEvents.ToArray();
            }
        }

        public IEvent[] AppliedEvents
        {
            get
            {
                return _appliedEvents.ToArray();
            }
        }

    }
}
