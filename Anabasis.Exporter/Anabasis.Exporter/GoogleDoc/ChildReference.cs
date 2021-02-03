using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Exporter
{
  public class ChildReference
  {
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("childLink")]
    public string ChildLink { get; set; }
  }
}
