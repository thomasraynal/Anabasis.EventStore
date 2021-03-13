using Anabasis.Actor;
using Anabasis.Actor.Exporter.Exporters.Bobby;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Bobby
{
  public class BobbyExporter : BaseActor
  {

    public BobbyExporter(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
    }

    public async Task Handle(DocumentBuildRequested exportDocumentBuilder)
    {

      var documentBuilders = exportDocumentBuilder.DocumentBuilderBatch.Select(builder => new BobbyDocumentBuilder(builder.documentUrl, builder.documentHeading));

      var bobbyAnabasisDocuments = new List<BobbyAnabasisDocument>();

      foreach (var documentBuilder in documentBuilders.OrderBy(builder => builder.MainTitle))
      {

        try
        {

          var items = documentBuilder.BuildItems();

          bobbyAnabasisDocuments.AddRange(items);

        }
        catch (Exception)
        {

          Emit(new DocumentCreationFailed(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId, exportDocumentBuilder.DocumentId)).Wait();

          break;

        }

      }

      var path = Path.GetFullPath($"{exportDocumentBuilder.DocumentId}");

      File.WriteAllText(path, JsonConvert.SerializeObject(bobbyAnabasisDocuments));

      await Emit(new DocumentCreated(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId, exportDocumentBuilder.DocumentId, new Uri(path)));

    }
  }
}
