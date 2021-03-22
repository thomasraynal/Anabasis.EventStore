using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo.Events
{
  public class TradeMarketValueChanged : BaseAggregateEvent<long, Trade>
  {
    public decimal MarketPrice { get; set; }

    public TradeMarketValueChanged(long entityId) : base(entityId)
    {
    }

    protected override void ApplyInternal(Trade entity)
    {
      entity.MarketPrice = MarketPrice;
    }
  }
}
