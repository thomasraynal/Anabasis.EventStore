using System;

namespace Anabasis.Exporter.Bobby
{

  public class BobbyQuote : IEquatable<BobbyQuote>
  {
 
    public string Id { get; set; }
    public string Text { get; set; }
    public string Author { get; set; }
    public string Tag { get; set; }

    public string[] Tags
    {
      get
      {
        return Tag.Split("-");
      }
    }

    public override bool Equals(object obj)
    {
      if (!(obj is BobbyQuote)) return false;
      return (obj as BobbyQuote).GetHashCode() == base.GetHashCode();
    }

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    public bool Equals(BobbyQuote other)
    {
      return other.Id == Id;
    }
  }
}
