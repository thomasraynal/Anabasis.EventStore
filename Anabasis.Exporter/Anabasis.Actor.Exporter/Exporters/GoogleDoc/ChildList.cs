using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Exporter
{
  public class ChildList
  {
    [JsonProperty("items")]
    public ChildReference[] ChildReferences { get; set; }

    [JsonProperty("nextLink")]
    public string NextLink { get; set; }
  }
}
