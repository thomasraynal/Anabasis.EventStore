using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{

    public interface IAggregateEvent<TAggregate> : IEvent where TAggregate : class, IAggregate
    {
        long EventNumber { get; set; }
        void Apply(TAggregate aggregate);
    }
}
