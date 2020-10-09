using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace Anabasis.Exporter
{
  [JsonConverter(typeof(JsonPathConverter))]
  public class ParagraphLite
  {
    [JsonProperty(PropertyName = "paragraph.elements[0].textRun.textStyle.bold")]
    public bool Bold { get; set; }

    [JsonProperty(PropertyName = "paragraph.elements[0].textRun.content")]
    public string Content { get; set; }

    [JsonProperty(PropertyName = "paragraph.elements[0].textRun.textStyle.link.url")]
    public string Url { get; set; }

    [JsonProperty(PropertyName = "paragraph.elements[0].textRun.textStyle.italic")]
    public bool Italic { get; set; }

    [JsonProperty(PropertyName = "paragraph.elements[0].textRun.textStyle.underline")]
    public bool Underline { get; set; }


    public DocumentItem ToDocumentItem(DocumentLite documentLite, string rootDocumentid)
    {
      var documentItem = new DocumentItem()
      {
        Id = (Bold || Italic) ? Content.GetReadableId() : $"{Guid.NewGuid()}",
        IsMainTitle = Bold,
        IsSecondaryTitle = Italic,
        IsEmphasis = Underline,
        Content = Content?.Trim('\n'),
        IsUrl = !string.IsNullOrEmpty(Url),
        DocumentId = rootDocumentid,
      };

      return documentItem;
    }

  }
}
