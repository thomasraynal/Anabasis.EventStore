using Anabasis.EventStore;
using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.EventStore.Integration.Tests
{
    public class CurrencyPairPriceChanged : BaseAggregateEvent<CurrencyPair>
    {
        public CurrencyPairPriceChanged(string ccyPairId, string traderId, double ask, double bid, double mid, double spread) : base(ccyPairId, Guid.NewGuid())
        {
            Ask = ask;
            Bid = bid;
            Mid = mid;
            Spread = spread;
            TraderId = traderId;
        }

        public double Ask { get; set; }
        public double Bid { get; set; }
        public double Mid { get; set; }
        public double Spread { get; set; }
        public string TraderId { get; set; }

        public override void Apply(CurrencyPair aggregate)
        {
            aggregate.Ask = Ask;
            aggregate.Bid = Bid;
            aggregate.Mid = Mid;
            aggregate.Spread = Spread;
        }
    }
}
