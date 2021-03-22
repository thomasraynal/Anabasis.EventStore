using System;
using DynamicData;


namespace Anabasis.EventStore.Demo
{
    public interface INearToMarketService
    {
        IObservable<IChangeSet<Trade, long>> Query(Func<decimal> percentFromMarket);
    }
}
