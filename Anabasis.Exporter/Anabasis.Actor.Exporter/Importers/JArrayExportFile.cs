using Anabasis.Importer;
using Newtonsoft.Json.Linq;

namespace Anabasis.Importer
{
  public class JArrayExportFile : BaseExportFile
  {
    public override void Append(string item)
    {
      var jArray = JToken.Parse(item);

      foreach (var child in jArray.Children())
      {
        child.WriteTo(JsonTextWriter);
      }
    }
  }
}
