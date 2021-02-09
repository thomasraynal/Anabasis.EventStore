using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using HtmlAgilityPack;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Bobby
{
  public class BobbyExporter : BaseActor
  {
    private const string quotesIndex = "http://bobbymedit.fr/table-des-matieres/";
    private readonly PolicyBuilder _policyBuilder;

    public BobbyExporter(IMediator simpleMediator) : base(simpleMediator)
    {
      _policyBuilder = Policy.Handle<Exception>();
    }

    public override string StreamId => StreamIds.Bobby;

    public Task Handle(StartExportRequest startExport)
    {

      var htmlWeb = new HtmlWeb();
      var documentBuilders = new List<BobbyDocumentBuilder>();

      var retryPolicy = _policyBuilder.WaitAndRetry(5, (_) => TimeSpan.FromSeconds(10));

      var doc = retryPolicy.Execute(() =>
      {
        return htmlWeb.Load(quotesIndex);
      });

      var currentHeading = string.Empty;

      foreach (var node in doc.DocumentNode.SelectNodes("//a"))
      {
        var href = node.Attributes["href"];

        if (null != href)
        {
          try
          {
            if (node.ParentNode.Name == "strong")
            {
              currentHeading = href.Value;
            }

            else
            {
              var documentBuilder = new BobbyDocumentBuilder(href.Value, currentHeading);

              if (documentBuilders.Contains(documentBuilder))
              {
                documentBuilders.Remove(documentBuilder);
              }

              documentBuilders.Add(documentBuilder);
            }

          }

          catch (Exception)
          {
          }
        }

      }

      var expectedDocuments = documentBuilders.GroupBy(builder => builder.DocumentId).Select(group => group.Key).ToArray();

      Mediator.Emit(new ExportStarted(startExport.CorrelationID, expectedDocuments, startExport.StreamId, startExport.TopicId));

      Parallel.ForEach(documentBuilders.OrderBy(builder=> builder.MainTitle).GroupBy(builder => builder.DocumentId), new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (parserBatch) =>
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

            Mediator.Emit(new DocumentDefined(startExport.CorrelationID, startExport.StreamId, startExport.TopicId, $"{parser.Url}"));

            return anabasisDocumentItems;

          }
          catch (Exception)
          {
            Mediator.Emit(new DocumentCreationFailed(startExport.CorrelationID, startExport.StreamId, startExport.TopicId, $"{parser.Url}"));

            return null;
          }

        }).ToArray();


        aggregatedDocument.DocumentItems = anabasisDocumentItems;


        Mediator.Emit(new DocumentCreated(startExport.CorrelationID, startExport.StreamId, startExport.TopicId, aggregatedDocument));


      });


      return Task.CompletedTask;

    }
  }
}
