namespace Anabasis.Common
{
    public interface IStatefulActor<TAggregate, TAggregateCacheConfiguration> : IActor, IAggregateCache<TAggregate> 
        where TAggregate : IAggregate, new()
        where TAggregateCacheConfiguration : IAggregateCacheConfiguration<TAggregate>
    {
    }
}
