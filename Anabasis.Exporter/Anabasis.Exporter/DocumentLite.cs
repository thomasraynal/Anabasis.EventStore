using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Exporter
{
    [JsonConverter(typeof(JsonPathConverter))]
    public class DocumentLite
    {
        [JsonProperty("documentId")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "body.content")]
        public ParagraphLite[] Paragraphs { get; set; }

    }
}
