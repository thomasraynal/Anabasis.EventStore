using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Shared
{

    public interface IMutable<TKey, TEntity> : IEntity<TKey> where TEntity : IAggregate<TKey>
    {
        void Apply(TEntity entity);
    }
}
