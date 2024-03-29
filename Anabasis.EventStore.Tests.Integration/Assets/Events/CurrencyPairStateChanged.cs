
using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.EventStore.Integration.Tests
{
  public class CurrencyPairStateChanged : BaseAggregateEvent<CurrencyPair>
  {
    public CurrencyPairStateChanged(string ccyPairId, string traderId, CcyPairState state): base(ccyPairId, Guid.NewGuid())
    {
      State = state;
      TraderId = traderId;
    }

    public CcyPairState State { get; set; }

    public string TraderId { get; set; }

    public override void Apply(CurrencyPair entity)
    {
      entity.State = State;
    }
  }
}
