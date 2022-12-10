namespace Anabasis.Common
{
    public interface IStatefulActor<TAggregate, TAggregateCacheConfiguration> : IAnabasisActor, IAggregateCache<TAggregate> 
        where TAggregate : IAggregate, new()
        where TAggregateCacheConfiguration : IAggregateCacheConfiguration
    {
    }
}
