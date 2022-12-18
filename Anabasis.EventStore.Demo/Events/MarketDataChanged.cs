using Anabasis.Common;
using Anabasis.Common.Contracts;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
    public class MarketDataBusMessage : BaseMessage
    {
        public MarketDataBusMessage(IEvent content, Guid? traceId = null) : base(Guid.NewGuid(), content, traceId)
        {
        }

        protected override Task AcknowledgeInternal()
        {
            return Task.CompletedTask;
        }

        protected override Task NotAcknowledgeInternal(string reason = null)
        {
            return Task.CompletedTask;
        }
    }
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
