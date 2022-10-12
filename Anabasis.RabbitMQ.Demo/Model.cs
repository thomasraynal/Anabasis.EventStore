using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo
{
    public class MarketDataChanged : BaseAggregateEvent<MarketData>
    {
        public double Bid { get; set; }
        public double Offer { get; set; }

        public MarketDataChanged(string ccyPair, Guid correlationId) : base(ccyPair, correlationId)
        {
        }

        public override void Apply(MarketData entity)
        {
            entity.Bid = Bid;
            entity.Offer = Offer;
        }
    }

    public class MarketData : BaseAggregate
    {
        public MarketData(string instrument, double bid, double offer)
        {
            EntityId = instrument;
            Bid = bid;
            Offer = offer;
            TimestampUtc = DateTime.UtcNow;
        }

        public MarketData()
        {
            TimestampUtc = DateTime.UtcNow;
        }

        public double Bid { get; set; }
        public double Offer { get; set; }
        public DateTime TimestampUtc { get; set; }


        public override bool Equals(object obj)
        {
            return obj is MarketData && obj.GetHashCode() == GetHashCode();
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (EntityId != null ? EntityId.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Bid.GetHashCode();
                hashCode = (hashCode * 397) ^ Offer.GetHashCode();
                return hashCode;
            }
        }


        public static MarketData operator +(MarketData left, double pipsValue)
        {
            var bid = left.Bid + pipsValue;
            var offer = left.Offer + pipsValue;
            return new MarketData(left.EntityId, bid, offer);
        }

        public static MarketData operator -(MarketData left, double pipsValue)
        {
            var bid = left.Bid - pipsValue;
            var offer = left.Offer - pipsValue;
            return new MarketData(left.EntityId, bid, offer);
        }

        public static bool operator >=(MarketData left, MarketData right)
        {
            return left.Bid >= right.Bid;
        }

        public static bool operator <=(MarketData left, MarketData right)
        {
            return left.Bid <= right.Bid;
        }

        public static bool operator >(MarketData left, MarketData right)
        {
            return left.Bid > right.Bid;
        }

        public static bool operator <(MarketData left, MarketData right)
        {
            return left.Bid < right.Bid;
        }

        public static bool operator ==(MarketData left, MarketData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MarketData left, MarketData right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return $"{EntityId}, {Bid}/{Offer}";
        }
    }
}
