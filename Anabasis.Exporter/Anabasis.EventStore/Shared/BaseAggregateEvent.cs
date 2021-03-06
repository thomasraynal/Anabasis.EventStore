using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public abstract class BaseAggregateEvent<TKey, TEntity> : IMutable<TKey, TEntity> where TEntity : IAggregate<TKey>
  {
    public TKey EntityId { get; set; }
    public Guid EventId { get; set; }
    protected abstract void ApplyInternal(TEntity entity);

    protected BaseAggregateEvent()
    {
      EventId = Guid.NewGuid();
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
