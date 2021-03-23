using System;

namespace Anabasis.EventStore.Demo
{
  //https://github.com/RolandPheasant/Dynamic.Trader
  public class MarketData : BaseAggregate<string>, IEquatable<MarketData>
  {
    public MarketData(string instrument, decimal bid, decimal offer)
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

    public decimal Bid { get; set; }
    public decimal Offer { get; set; }
    public DateTime TimestampUtc { get; set; }

    #region Equality

    public bool Equals(MarketData other)
    {
      return string.Equals(EntityId, other.EntityId) && Bid == other.Bid && Offer == other.Offer;
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) return false;
      return obj is MarketData && Equals((MarketData)obj);
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


    public static MarketData operator +(MarketData left, decimal pipsValue)
    {
      var bid = left.Bid + pipsValue;
      var offer = left.Offer + pipsValue;
      return new MarketData(left.EntityId, bid, offer);
    }

    public static MarketData operator -(MarketData left, decimal pipsValue)
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

    #endregion

    public override string ToString()
    {
      return $"{EntityId}, {Bid}/{Offer}";
    }
  }
}
