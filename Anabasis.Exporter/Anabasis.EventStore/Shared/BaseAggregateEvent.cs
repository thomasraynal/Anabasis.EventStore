using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Shared
{
  public abstract class BaseAggregateEvent<TKey, TEntity> : IEvent, IMutable<TKey, TEntity> where TEntity : IAggregate<TKey>
  {
    public TKey EntityId { get; set; }
    public Guid EventID { get; set; }
    public Guid CorrelationID { get; set; }

    protected abstract void ApplyInternal(TEntity entity);

    [JsonConstructor]
    private protected BaseAggregateEvent()
    {
    }

    protected BaseAggregateEvent(TKey entityId, Guid correlationId)
    {
      EventID = Guid.NewGuid();
      CorrelationID = correlationId;
      EntityId = entityId;
    }

    public void Apply(TEntity entity)
    {
      ApplyInternal(entity);
      entity.EntityId = EntityId;
    }

    public string StreamId
    {
      get
      {
        return EntityId.ToString();
      }
      set { }
    }

  }
}
