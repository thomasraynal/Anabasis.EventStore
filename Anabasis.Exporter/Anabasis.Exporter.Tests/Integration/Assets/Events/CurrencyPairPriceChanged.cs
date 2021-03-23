using Anabasis.EventStore;
using System;

namespace Anabasis.Tests.Integration
{
  public class CurrencyPairPriceChanged : BaseAggregateEvent<string, CurrencyPair>
  {
    public CurrencyPairPriceChanged(string ccyPairId, string traderId, double ask, double bid, double mid, double spread) : base(ccyPairId, Guid.NewGuid())
    {
      Ask = ask;
      Bid = bid;
      Mid = mid;
      Spread = spread;
      TraderId = traderId;
    }

    public double Ask { get; set; }
    public double Bid { get; set; }
    public double Mid { get; set; }
    public double Spread { get; set; }
    public string TraderId { get; set; }

    protected override void ApplyInternal(CurrencyPair aggregate)
    {
      aggregate.Ask = Ask;
      aggregate.Bid = Bid;
      aggregate.Mid = Mid;
      aggregate.Spread = Spread;
    }
  }
}
