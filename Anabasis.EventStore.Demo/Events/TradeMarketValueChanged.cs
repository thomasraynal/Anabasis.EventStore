using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class TradeMarketValueChanged : BaseAggregateEvent<long, Trade>
    {
        public decimal MarketPrice { get; set; }

        public TradeMarketValueChanged(long entityId, Guid correlationId) : base(entityId, correlationId)
        {
        }

        protected override void ApplyInternal(Trade entity)
        {
            entity.MarketPrice = MarketPrice;
        }
    }
}
