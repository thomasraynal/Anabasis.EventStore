
using Anabasis.EventStore;

namespace Anabasis.Tests.Integration
{
  public class CurrencyPairStateChanged : BaseAggregateEvent<string, CurrencyPair>
  {
    public CurrencyPairStateChanged(string ccyPairId, string traderId, CcyPairState state)
    {
      State = state;
      TraderId = traderId;
      EntityId = ccyPairId;
    }

    public CcyPairState State { get; set; }

    public string TraderId { get; set; }

    protected override void ApplyInternal(CurrencyPair entity)
    {
      entity.State = State;
    }
  }
}
