using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Shared
{

    public interface IMutation<TKey, TEntity> : IEntity<TKey> where TEntity : IAggregate<TKey>
    {
        bool IsCommand { get; }
        void Apply(TEntity entity);
    }
}
