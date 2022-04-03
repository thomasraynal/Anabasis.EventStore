using System.Diagnostics.CodeAnalysis;

namespace Anabasis.Common
{

    public interface IAggregateEvent<TAggregate> : IEvent where TAggregate : class, IAggregate
    {
        long EventNumber { get; set; }
        void Apply([NotNull] TAggregate aggregate);
    }
}
