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

    public string Title { get; set; }
    public bool IsMainTitle { get; set; }
    public bool IsEmphasis { get; set; }
    public string MainTitleId { get; set; }
    public string DocumentId { get; set; }
    public int Position { get; set; }
    public BobbyAnabasisDocument[] Children { get; set; }

    public override bool Equals(object obj)
    {
      return obj is BobbyAnabasisDocument document &&
             base.Equals(obj) &&
             Author == document.Author &&
             Content == document.Content &&
             Id == document.Id &&
             IsRootDocument == document.IsRootDocument &&
             ParentId == document.ParentId &&
             Tag == document.Tag &&
             Type == document.Type &&
             IsMainTitle == document.IsMainTitle &&
             IsEmphasis == document.IsEmphasis &&
             MainTitleId == document.MainTitleId &&
             DocumentId == document.DocumentId &&
             Position == document.Position;
    }

    public override int GetHashCode()
    {
      HashCode hash = new HashCode();
      hash.Add(base.GetHashCode());
      hash.Add(Author);
      hash.Add(Content);
      hash.Add(Id);
      hash.Add(IsRootDocument);
      hash.Add(ParentId);
      hash.Add(Tag);
      hash.Add(Type);
      hash.Add(IsMainTitle);
      hash.Add(IsEmphasis);
      hash.Add(MainTitleId);
      hash.Add(DocumentId);
      hash.Add(Position);
      return hash.ToHashCode();
    }
  }
}
