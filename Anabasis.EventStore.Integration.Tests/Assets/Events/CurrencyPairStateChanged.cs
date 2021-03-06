
using Anabasis.EventStore;
using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.EventStore.Tests.Integration
{
  public class CurrencyPairStateChanged : BaseAggregateEvent<string, CurrencyPair>
  {
    public CurrencyPairStateChanged(string ccyPairId, string traderId, CcyPairState state): base(ccyPairId, Guid.NewGuid())
    {
      State = state;
      TraderId = traderId;
    }

    public CcyPairState State { get; set; }

    public string TraderId { get; set; }

    protected override void ApplyInternal(CurrencyPair entity)
    {
      entity.State = State;
    }
  }
}
