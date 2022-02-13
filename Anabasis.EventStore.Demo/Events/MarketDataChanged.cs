using Anabasis.Common;
using System;

namespace Anabasis.EventStore.Demo
{
    public class MarketDataChanged : BaseAggregateEvent<MarketData>
    {
        public decimal Bid { get; set; }
        public decimal Offer { get; set; }

        public MarketDataChanged(string entityId, Guid correlationId) : base(entityId, correlationId)
        {
        }

        public override void Apply(MarketData entity)
        {
            entity.Bid = Bid;
            entity.Offer = Offer;
        }
    }
}
