using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo.Events
{
  public class MarketDataChanged : BaseAggregateEvent<string, MarketData>
  {
    public decimal Bid { get; set; }
    public decimal Offer { get; set; }

    public MarketDataChanged(string entityId) : base(entityId)
    {
    }

    protected override void ApplyInternal(MarketData entity)
    {
      entity.Bid = Bid;
      entity.Offer = Offer;
    }
  }
}
