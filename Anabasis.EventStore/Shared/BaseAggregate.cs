using Anabasis.Common;
using Newtonsoft.Json;
using System.Collections.Generic;


namespace Anabasis.EventStore.Shared
{

  public abstract class BaseAggregate<TKey> : IAggregate<TKey>
  {
    private readonly List<IEntity<TKey>> _pendingEvents = new();
    private readonly List<IEntity<TKey>> _appliedEvents = new();

    public TKey EntityId { get; set; }

    public int Version { get; set; } = -1;

    [JsonProperty]
    public int VersionFromSnapshot { get; set; } = -1;

    public void Mutate<TEntity>(IMutation<TKey, TEntity> @event) where TEntity : class, IAggregate<TKey>
    {
      @event.Apply(this as TEntity);
      Version++;
    }

    public void ApplyEvent(IEntity<TKey> @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = true)
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

    public ICollection<IEntity<TKey>> GetPendingEvents()
    {
      return _pendingEvents;
    }

    public void ClearPendingEvents()
    {
      _pendingEvents.Clear();
    }

    public string StreamId
    {
      get
      {
        return $"{EntityId}";
      }
      set { }
    }

    public IEntity<TKey>[] PendingEvents
    {
      get
      {
        return _pendingEvents.ToArray();
      }

    }

    public IEntity<TKey>[] AppliedEvents
    {
      get
      {
        return _appliedEvents.ToArray();
      }

    }

  }
}
