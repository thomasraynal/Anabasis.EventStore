using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class TradePriceChanged : BaseAggregateEvent<Trade>
    {
        public decimal MarketPrice { get; set; }
        public decimal PercentFromMarket { get; set; }

        public TradePriceChanged(string entityId, Guid correlationId) : base($"{entityId}", correlationId)
        {
        }

        public override void Apply(Trade entity)
        {
            entity.MarketPrice = MarketPrice;
            entity.PercentFromMarket = PercentFromMarket;
        }
    }
}
