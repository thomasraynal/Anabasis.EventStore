using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Anabasis.Tests.POC
{
  public class AnabasisDocument : IAnabasisDocument
  {
    public AnabasisDocument()
    {
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public string Id { get; set; }
    public bool IsUrl { get; set; }
    public bool IsMainTitle { get; set; }
    public string Content { get; set; }
    public bool IsSecondaryTitle { get; set; }
    public bool IsEmphasis { get; set; }
    public string MainTitleId { get; set; }
    public string SecondaryTitleId { get; set; }
    public string ParentId { get; set; }
    public string DocumentId { get; set; }
    public int Position { get; set; }
    public string Author { get; set; }
    public string Tag { get; set; }

    public bool IsRootDocument => ParentId == null;

    public override bool Equals(object obj)
    {
      return obj is AnabasisDocument item &&
             Id == item.Id &&
             IsUrl == item.IsUrl &&
             IsMainTitle == item.IsMainTitle &&
             Content == item.Content &&
             IsSecondaryTitle == item.IsSecondaryTitle &&
             IsEmphasis == item.IsEmphasis &&
             MainTitleId == item.MainTitleId &&
             SecondaryTitleId == item.SecondaryTitleId &&
             ParentId == item.ParentId &&
             DocumentId == item.DocumentId &&
             Position == item.Position &&
             IsRootDocument == item.IsRootDocument;
    }

    public override int GetHashCode()
    {
      HashCode hash = new HashCode();
      hash.Add(Id);
      hash.Add(IsUrl);
      hash.Add(IsMainTitle);
      hash.Add(Content);
      hash.Add(IsSecondaryTitle);
      hash.Add(IsEmphasis);
      hash.Add(MainTitleId);
      hash.Add(SecondaryTitleId);
      hash.Add(ParentId);
      hash.Add(DocumentId);
      hash.Add(Position);
      hash.Add(IsRootDocument);
      return hash.ToHashCode();
    }
  }
}
