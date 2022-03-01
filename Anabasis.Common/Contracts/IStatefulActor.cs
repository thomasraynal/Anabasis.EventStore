namespace Anabasis.Common
{
    public interface IStatefulActor<TAggregate> : IActor where TAggregate : IAggregate, new()
    {
        IAggregateCache<TAggregate> State { get; }
    }
}
