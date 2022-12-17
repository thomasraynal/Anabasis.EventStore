using Anabasis.Common;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
    public class MarketDataBusMessage : IMessage
    {
        public MarketDataBusMessage(IEvent content)
        {
            Content = content;
        }

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content { get; }

        public Guid? TraceId { get; }

        public bool IsAcknowledged => throw new NotImplementedException();

        public IObservable<bool> OnAcknowledged => throw new NotImplementedException();

        public Task Acknowledge()
        {
            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string reason=null)
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
