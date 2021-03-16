using Anabasis.Actor;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Illiad
{

  public class IlliadExporter : BaseActor
  {
    private JsonSerializerSettings _jsonSerializerSettings;

    public IlliadExporter(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
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

    public async Task Handle(ExportDocumentCommand exportDocumentRequest)
    {

      var documentBuilder = new IlliadDocumentBuilder(
        exportDocumentRequest.DocumentId,
        exportDocumentRequest.DocumentUrl);

      var anabasisDocument = documentBuilder.Build();

      var path = Path.GetFullPath($"{documentBuilder.DocumentId}");

      File.WriteAllText(path, JsonConvert.SerializeObject(anabasisDocument, _jsonSerializerSettings));

      await Emit(new DocumentCreated(exportDocumentRequest.CorrelationID, exportDocumentRequest.StreamId, documentBuilder.DocumentId, new Uri(path)));


    }
  }
}
