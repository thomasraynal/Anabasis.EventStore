using System.Linq;
using Anabasis.Common;
using Anabasis.EventStore.Shared;

namespace Anabasis.EventStore.Integration.Tests
{
  public class CurrencyPair : BaseAggregate
    {

        public CcyPairState State { get; set; }
        public double Ask { get; set; }
        public double Bid { get; set; }
        public double Mid { get; set; }
        public double Spread { get; set; }

        public override string ToString()
        {
            return $"{this.EntityId}({AppliedEvents.Count()} event(s))";
        }
    }
}
