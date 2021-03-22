using DynamicData;


namespace Anabasis.EventStore.Demo
{
    public interface ITradeService
    {
        IObservableCache<Trade, long> All { get; }
        IObservableCache<Trade, long> Live { get; }
    }
}