using Newtonsoft.Json;

namespace Anabasis.Common
{
    public interface IAggregate : IEntity
    {
        int Version { get; }
        int VersionFromSnapshot { get; set; }
        void ApplyEvent(IEntity @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = false);
        [JsonIgnore]
        IEntity[] PendingEvents { get; }
        [JsonIgnore]
        IEntity[] AppliedEvents { get; }
        void ClearPendingEvents();
        void SetEntityId(string entityId);
    }
}
