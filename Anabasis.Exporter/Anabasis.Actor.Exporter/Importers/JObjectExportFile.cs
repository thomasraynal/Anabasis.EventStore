using Anabasis.Importer;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Importer
{
  public class JObjectExportFile : BaseExportFile
  {
    public override void Append(string item)
    {
      var jObject = JObject.Parse(item);

      jObject.WriteTo(JsonTextWriter);
    }
  }
}
