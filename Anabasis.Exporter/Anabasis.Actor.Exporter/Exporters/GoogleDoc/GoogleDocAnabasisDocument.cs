using Anabasis.Common;
using System;
using System.Text;

namespace Anabasis.Exporter
{
  public class GoogleDocAnabasisDocument : BaseAnabasisDocument
  {
    public GoogleDocAnabasisDocument()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public string Title { get; set; }
    public bool IsUrl { get; set; }
    public bool IsMainTitle { get; set; }
    public bool IsSecondaryTitle { get; set; }
    public bool IsEmphasis { get; set; }
    public string MainTitleId { get; set; }
    public string SecondaryTitleId { get; set; }
    public int Position { get; set; }
    public bool IsRootDocument => ParentId == null;
    public string ParentId { get; set; }

    public GoogleDocAnabasisDocument[] Children { get; set; }

    public override bool Equals(object obj)
    {
      return obj is GoogleDocAnabasisDocument document &&
             base.Equals(obj) &&
             Author == document.Author &&
             Content == document.Content &&
             Id == document.Id &&
             IsRootDocument == document.IsRootDocument &&
             ParentId == document.ParentId &&
             Tag == document.Tag &&
             Type == document.Type &&
             IsUrl == document.IsUrl &&
             IsMainTitle == document.IsMainTitle &&
             IsSecondaryTitle == document.IsSecondaryTitle &&
             IsEmphasis == document.IsEmphasis &&
             MainTitleId == document.MainTitleId &&
             SecondaryTitleId == document.SecondaryTitleId &&
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
      hash.Add(IsUrl);
      hash.Add(IsMainTitle);
      hash.Add(IsSecondaryTitle);
      hash.Add(IsEmphasis);
      hash.Add(MainTitleId);
      hash.Add(SecondaryTitleId);
      hash.Add(Position);
      return hash.ToHashCode();
    }
  }
}
