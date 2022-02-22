using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{

    public interface IAggregateEvent<TAggregate> : IEvent where TAggregate : class, IAggregate
    {
        void Apply(TAggregate aggregate);
    }
}
