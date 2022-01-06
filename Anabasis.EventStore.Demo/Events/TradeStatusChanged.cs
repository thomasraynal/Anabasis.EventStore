using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class TradeStatusChanged : BaseAggregateEvent<Trade>
    {
        public TradeStatus Status { get; set; }

        public TradeStatusChanged(string entityId, Guid correlationId) : base($"{entityId}", correlationId)
        {
        }

        protected override void ApplyInternal(Trade entity)
        {
            entity.Status = Status;
        }
    }
}
