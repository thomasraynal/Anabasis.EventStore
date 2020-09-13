using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Exporter
{
  public class DocumentItem
  {
    public DocumentItem()
    {
      Id = $"{Guid.NewGuid()}";
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
    public int Position { get;  set; }
  }
}
