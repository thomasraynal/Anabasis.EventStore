using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
  public class MarketDataChanged : BaseAggregateEvent<string, MarketData>
  {
    public decimal Bid { get; set; }
    public decimal Offer { get; set; }

    public MarketDataChanged(string entityId, Guid correlationId) : base(entityId, correlationId)
    {
    }

    protected override void ApplyInternal(MarketData entity)
    {
      entity.Bid = Bid;
      entity.Offer = Offer;
    }
  }
}
