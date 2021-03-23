using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Anabasis.EventStore.Demo
{
  public class Trade : BaseAggregate<long>
  {
    public Trade()
    {
    }

    public Trade(Trade trade, TradeStatus closed)
    {
      EntityId = trade.EntityId;
      Customer = trade.Customer;
      CurrencyPair = trade.CurrencyPair;
      Status = closed;
      BuyOrSell = trade.BuyOrSell;
      TradePrice = trade.TradePrice;
      Amount = trade.Amount;
      MarketPrice = trade.MarketPrice;

    }

    public Trade(long id, string bank, string ccyPair, TradeStatus status, BuyOrSell buySell, decimal tradePrice, int amount) 
    {
      EntityId = id;
      Customer = bank;
      CurrencyPair = ccyPair;
      Status = status;
      BuyOrSell = buySell;
      TradePrice = tradePrice;
      Amount = amount;
    }

    public Trade(long id, string bank, string ccyPair, TradeStatus status, BuyOrSell buySell, decimal tradePrice, int amount, DateTime timeStamp)
    {
      EntityId = id;
      Customer = bank;
      CurrencyPair = ccyPair;
      Status = status;
      BuyOrSell = buySell;
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



  }
}
