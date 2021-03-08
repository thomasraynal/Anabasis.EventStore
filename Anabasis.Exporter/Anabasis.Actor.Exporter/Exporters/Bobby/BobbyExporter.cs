using Anabasis.Actor;
using Anabasis.Actor.Exporter.Exporters.Bobby;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Polly;
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

    public async Task Handle(ExportDocumentBuilder exportDocumentBuilder)
    {

      var documentBuilders = exportDocumentBuilder.DocumentBuilderBatch.Select(builder => new BobbyDocumentBuilder(builder.documentUrl, builder.documentHeading));

      foreach (var parserBatch in documentBuilders.OrderBy(builder => builder.MainTitle).GroupBy(builder => builder.DocumentId))
      {

        var aggregatedDocumentId = parserBatch.Key;

        var aggregatedDocument = new AnabasisDocument()
        {
          Id = aggregatedDocumentId,
          Title = parserBatch.Key
        };

        var anabasisDocumentItems = parserBatch.SelectMany(parser =>
        {

          try
          {
            var anabasisDocumentItems = parser.BuildItems(aggregatedDocument);

            Emit(new DocumentDefined(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId, exportDocumentBuilder.TopicId, $"{parser.Url}")).Wait();

            return anabasisDocumentItems;

          }
          catch (Exception)
          {
            Emit(new DocumentCreationFailed(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId, exportDocumentBuilder.TopicId, $"{parser.Url}")).Wait();

            return null;
          }

        }).ToArray();


        aggregatedDocument.DocumentItems = anabasisDocumentItems;

        var path = Path.GetFullPath($"{aggregatedDocument.Id}");

        File.WriteAllText(path, JsonConvert.SerializeObject(aggregatedDocument));

        await Emit(new DocumentCreated(exportDocumentBuilder.CorrelationID, exportDocumentBuilder.StreamId, exportDocumentBuilder.TopicId, aggregatedDocument.Id, new Uri(path)));

      }
    }
  }
}
