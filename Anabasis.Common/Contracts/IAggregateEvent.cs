using System.Diagnostics.CodeAnalysis;

namespace Anabasis.Common
{
    public interface IAggregateEvent: IEvent
    {
        long EventNumber { get; set; }
    }
    public interface IAggregateEvent<TAggregate> : IAggregateEvent where TAggregate : class, IAggregate
    {
        void Apply([NotNull] TAggregate aggregate);
    }
}
