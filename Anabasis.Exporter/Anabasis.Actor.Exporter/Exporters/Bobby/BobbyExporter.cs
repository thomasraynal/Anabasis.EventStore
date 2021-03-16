using Anabasis.Actor;
using Anabasis.Actor.Exporter.Exporters.Bobby;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Bobby
{
  public class BobbyExporter : BaseActor
  {
    private JsonSerializerSettings _jsonSerializerSettings;

    public BobbyExporter(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
      _jsonSerializerSettings = new JsonSerializerSettings()
      {
        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy()
        },
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented

      };
    }

    public async Task Handle(DocumentBuildRequested exportDocumentBuilder)
    {

      var documentBuilders = exportDocumentBuilder.DocumentBuilderBatch.Select(builder => new BobbyDocumentBuilder(builder.documentUrl, builder.documentHeading));

      var bobbyAnabasisDocuments = new List<BobbyAnabasisDocument>();

      foreach (var documentBuilder in documentBuilders.OrderBy(builder => builder.MainTitle))
      {
        var items = documentBuilder.BuildItems();

        bobbyAnabasisDocuments.AddRange(items);

      }

      var path = Path.GetFullPath($"{exportDocumentBuilder.DocumentId}");

      File.WriteAllText(path, JsonConvert.SerializeObject(bobbyAnabasisDocuments, _jsonSerializerSettings));

      await Emit(new DocumentCreated(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId, exportDocumentBuilder.DocumentId, new Uri(path)));

    }
  }
}
