using System.Text.Json.Serialization;

namespace Anabasis.EventStore.Shared
{
  public interface IAggregate<TKey> : IEntity<TKey>
  {
    int Version { get; }
    int VersionFromSnapshot { get; set; }
    void ApplyEvent(IEntity<TKey> @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = false);
    [JsonIgnore]
    IEntity<TKey>[] PendingEvents { get; }
    [JsonIgnore]
    IEntity<TKey>[] AppliedEvents { get; }
    void ClearPendingEvents();
  }
}
