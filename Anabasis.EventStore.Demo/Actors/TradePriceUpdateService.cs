using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Repository;

namespace Anabasis.EventStore.Demo
{
  public class TradePriceUpdateService : BaseStatefulActor<long, Trade>
  {

    public TradePriceUpdateService(IEventStoreAggregateRepository<long> eventStoreRepository, IEventStoreCache<long, Trade> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
    {
    }

    public async Task Handle(MarketDataChanged marketDataChanged)
    {

      foreach (var trade in State.GetCurrents().Where(trade => trade.CurrencyPair == marketDataChanged.EntityId))
      {
        await EmitEntityEvent(new TradePriceChanged(trade.EntityId, Guid.NewGuid())
        {
          MarketPrice = marketDataChanged.Bid,
          PercentFromMarket = Math.Round(((trade.TradePrice - trade.MarketPrice) / trade.MarketPrice) * 100, 4)
        });

      }

    }
  }
}
