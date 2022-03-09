using Newtonsoft.Json;

namespace Anabasis.Common
{
    public interface IAggregate : IHaveEntityId
    {
        long Version { get; }
        long VersionFromSnapshot { get; set; }
        void ApplyEvent<TAggregate>(IAggregateEvent<TAggregate> @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = false)
            where TAggregate : class, IAggregate;
        [JsonIgnore]
        IEvent[] PendingEvents { get; }
        [JsonIgnore]
        IEvent[] AppliedEvents { get; }
        void ClearPendingEvents();
        void SetEntityId(string entityId);
    }
}
