using System;
using System.Reactive.Linq;
using DynamicData;


namespace Anabasis.EventStore.Demo
{
    public class NearToMarketService : INearToMarketService
    {
        private readonly ITradeService _tradeService;

        public NearToMarketService(ITradeService tradeService)
        {
            _tradeService = tradeService ?? throw new ArgumentNullException(nameof(tradeService));
        }

        public IObservable<IChangeSet<Trade, long>> Query(Func<decimal> percentFromMarket)
        {
            if (percentFromMarket == null) throw new ArgumentNullException(nameof(percentFromMarket));

            return Observable.Create<IChangeSet<Trade, long>>
                (observer =>
                 {
                     var locker = new object();

                     bool Predicate(Trade t) => Math.Abs(t.PercentFromMarket) <= percentFromMarket();

                     //re-evaluate filter periodically
                     var reevaluator = Observable.Interval(TimeSpan.FromMilliseconds(250))
                         .Synchronize(locker)
                         .Select(_ => (Func<Trade, bool>) Predicate)
                         .StartWith((Func<Trade, bool>) Predicate); ;

                     //filter on live trades matching % specified
                     return _tradeService.All.Connect(trade => trade.Status == TradeStatus.Live)
                         .Synchronize(locker)
                         .Filter(reevaluator)
                         .SubscribeSafe(observer);
                 });
        }
    }
}
