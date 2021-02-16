using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public abstract class EventBase<TKey,TEntity> : IMutable<TKey,TEntity> where TEntity : IAggregate<TKey>
    {
        public string Name { get; set; }
        public TKey EntityId { get; set; }
        protected abstract void ApplyInternal(TEntity entity);

        protected EventBase()
        {
            Name = GetType().FullName;
        }

        public void Apply(TEntity entity)
        {
            ApplyInternal(entity);
            entity.EntityId = EntityId;
        }

        public virtual string GetStreamName()
        {
            return EntityId.ToString();
        }
    }
}
