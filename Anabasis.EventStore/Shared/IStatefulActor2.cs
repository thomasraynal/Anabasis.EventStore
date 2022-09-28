namespace Anabasis.Common
{
    public interface IStatefulActor2<TAggregate> : IActor, IAggregateCache<TAggregate> where TAggregate : IAggregate, new()
    {
    }
}
