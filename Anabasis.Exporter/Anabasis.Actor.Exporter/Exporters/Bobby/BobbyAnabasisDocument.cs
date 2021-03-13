using System;
using System.Text;

namespace Anabasis.Common
{
  public class BobbyAnabasisDocument : BaseAnabasisDocument
  {
    public BobbyAnabasisDocument()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public override bool Equals(object obj)
    {
      return obj is BobbyAnabasisDocument document &&
             base.Equals(obj) &&
             Author == document.Author &&
             Content == document.Content &&
             Id == document.Id &&
             Tag == document.Tag &&
             Type == document.Type;
    }

    public override int GetHashCode()
    {
      HashCode hash = new HashCode();
      hash.Add(base.GetHashCode());
      hash.Add(Author);
      hash.Add(Content);
      hash.Add(Id);
      hash.Add(Tag);
      hash.Add(Type);
      return hash.ToHashCode();
    }
  }
}
