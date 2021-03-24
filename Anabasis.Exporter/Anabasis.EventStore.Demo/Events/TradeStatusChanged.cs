using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo  
{
  public class TradeStatusChanged : BaseAggregateEvent<long, Trade>
  {
    public TradeStatus Status { get; set; }

    public TradeStatusChanged(long entityId, Guid correlationId) : base(entityId, correlationId)
    {
    }

    protected override void ApplyInternal(Trade entity)
    {
      entity.Status = Status;
    }
  }
}
