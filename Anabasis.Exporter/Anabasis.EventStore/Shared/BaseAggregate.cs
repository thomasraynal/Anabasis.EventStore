using System.Collections.Generic;


namespace Anabasis.EventStore
{

  public abstract class BaseAggregate<TKey> : IAggregate<TKey>
  {
    private readonly List<IEntityEvent<TKey>> _pendingEvents = new List<IEntityEvent<TKey>>();
    private readonly List<IEntityEvent<TKey>> _appliedEvents = new List<IEntityEvent<TKey>>();

    public TKey EntityId { get; set; }

    public int Version { get; private set; } = -1;
    public int VersionSnapShot { get; private set; } = -1;

    public void Mutate<TEntity>(IMutable<TKey, TEntity> @event) where TEntity : class, IAggregate<TKey>
    {
      @event.Apply(this as TEntity);
      Version++;
    }

    public void ApplyEvent(IEntityEvent<TKey> @event, bool saveAsPendingEvent = true, bool keepAppliedEventsOnAggregate = true)
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

    public ICollection<IEntityEvent<TKey>> GetPendingEvents()
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
        return EntityId.ToString();
      }
      set { }
    }

    public IEntityEvent<TKey>[] PendingEvents
    {
      get
      {
        return _pendingEvents.ToArray();
      }

    }

    public IEntityEvent<TKey>[] AppliedEvents
    {
      get
      {
        return _appliedEvents.ToArray();
      }

    }

  }
}
