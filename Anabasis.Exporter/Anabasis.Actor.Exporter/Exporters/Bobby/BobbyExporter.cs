using Anabasis.Actor;
using Anabasis.Actor.Exporter.Exporters.Bobby;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Newtonsoft.Json;
using System;
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

      var aggregatedDocument = new BobbyAnabasisDocument()
      {
        Id = exportDocumentBuilder.DocumentId,
        Title = exportDocumentBuilder.DocumentId,
        Tag = exportDocumentBuilder.DocumentId,
      };

      foreach (var documentBuilder in documentBuilders.OrderBy(builder => builder.MainTitle))
      {

        Emit(new TitleDefined(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId, aggregatedDocument.Id, aggregatedDocument.Title)).Wait();

        try
        {

          var items = documentBuilder.BuildItems(aggregatedDocument);

          aggregatedDocument.Children = aggregatedDocument.Children.Concat(items).ToArray();

        }
        catch (Exception)
        {

          Emit(new DocumentCreationFailed(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId, aggregatedDocument.Title)).Wait();

          break;

        }
      }

      var path = Path.GetFullPath($"{aggregatedDocument.Id}");

      File.WriteAllText(path, JsonConvert.SerializeObject(aggregatedDocument));

      await Emit(new DocumentCreated(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId,  aggregatedDocument.Title, new Uri(path)));

    }
  }
}
