using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Anabasis.EventStore.Demo
{
  public class Trade : BaseAggregate<long>
  {
    public Trade(long id)
    {
      EntityId = id;
    }

    public Trade(long id, string bank, string ccyPair, TradeStatus status, BuyOrSell buySell, decimal tradePrice, int amount) : this(id)
    {
      EntityId = id;
      Customer = bank;
      CurrencyPair = ccyPair;
      Status = status;
      BuyOrSell = BuyOrSell;
      TradePrice = tradePrice;
      Amount = amount;
    }

    public Trade(long id, string bank, string ccyPair, TradeStatus status, BuyOrSell buySell, decimal tradePrice, int amount, DateTime timeStamp) : this(id)
    {
      EntityId = id;
      Customer = bank;
      CurrencyPair = ccyPair;
      Status = status;
      BuyOrSell = BuyOrSell;
      TradePrice = tradePrice;
      Amount = amount;
      Timestamp = timeStamp;
    }

    public string CurrencyPair { get; set; }
    public string Customer { get; set; }
    public decimal TradePrice { get; set; }
    public decimal MarketPrice { get; set; }
    public decimal PercentFromMarket { get; set; }
    public decimal Amount { get; set; }
    public BuyOrSell BuyOrSell { get; set; }
    public TradeStatus Status { get; set; }
    public DateTime Timestamp { get; set; }


    //public void SetMarketPrice(decimal marketPrice)
    //{
    //    MarketPrice = marketPrice;
    //    PercentFromMarket = Math.Round(((TradePrice - MarketPrice) / MarketPrice) * 100, 4);
    //    ;
    //    _marketPriceChangedSubject.OnNext(marketPrice);
    //}
  }
}
