using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{

    public interface IMutation< TEntity> : IEntity where TEntity : IAggregate
    {
        bool IsCommand { get; }
        void Apply(TEntity entity);
    }
}
