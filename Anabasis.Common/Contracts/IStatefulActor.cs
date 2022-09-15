namespace Anabasis.Common
{
    public interface IStatefulActor<TAggregate> : IActor, IAggregateCache<TAggregate> where TAggregate : IAggregate, new()
    {
        IAggregateCache<TAggregate> State { get; }
    }
}
