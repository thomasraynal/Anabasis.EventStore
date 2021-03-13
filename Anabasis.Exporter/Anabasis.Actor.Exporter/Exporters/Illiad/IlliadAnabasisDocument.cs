using System;
using System.Text;

namespace Anabasis.Common
{
  public class IlliadAnabasisDocument : BaseAnabasisDocument
  {
    public IlliadAnabasisDocument()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public string Source { get; set; }

    public override bool Equals(object obj)
    {
      return obj is IlliadAnabasisDocument document &&
             base.Equals(obj) &&
             Author == document.Author &&
             Content == document.Content &&
             Id == document.Id &&
             Tag == document.Tag &&
             Type == document.Type &&
             Source == document.Source;
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
      hash.Add(Source);
      return hash.ToHashCode();
    }
  }
}
