using System.Linq;
using Anabasis.EventStore.Shared;

namespace Anabasis.EventStore.Tests.Integration
{
  public class CurrencyPair : BaseAggregate<string>
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
