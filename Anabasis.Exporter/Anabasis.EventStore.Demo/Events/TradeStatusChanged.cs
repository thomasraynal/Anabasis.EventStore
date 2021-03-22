using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo.Events
{
  public class TradeStatusChanged : BaseAggregateEvent<long, Trade>
  {
    public TradeStatus Status { get; set; }

    public TradeStatusChanged(long entityId) : base(entityId)
    {

    }

    protected override void ApplyInternal(Trade entity)
    {
      entity.Status = Status;
    }
  }
}
