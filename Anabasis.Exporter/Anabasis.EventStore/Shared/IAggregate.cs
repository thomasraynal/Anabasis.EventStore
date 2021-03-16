using System.Text.Json.Serialization;

namespace Anabasis.EventStore
{
  public interface IAggregate<TKey> : IEntityEvent<TKey>
  {
    int Version { get; }
    int VersionSnapShot { get; }
    void ApplyEvent(IEntityEvent<TKey> @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = false);
    [JsonIgnore]
    IEntityEvent<TKey>[] PendingEvents { get; }
    [JsonIgnore]
    IEntityEvent<TKey>[] AppliedEvents { get; }
    void ClearPendingEvents();
  }
}
