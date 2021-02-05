using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.Exporter.Bobby
{

  public class Quote : IEquatable<Quote>
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
      if (!(obj is Quote)) return false;
      return (obj as Quote).GetHashCode() == base.GetHashCode();
    }

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    public bool Equals(Quote other)
    {
      return other.Id == Id;
    }
  }
}
