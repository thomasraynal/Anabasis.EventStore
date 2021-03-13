using Anabasis.Common.Events;
using Anabasis.EventStore;
using Anabasis.Exporter.Bobby;
using HtmlAgilityPack;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Actor.Exporter.Exporters.Bobby
{
  public class BobbyDispatcher : BaseActor
  {
    private const string quotesIndex = "http://bobbymedit.fr/table-des-matieres/";
    private readonly PolicyBuilder _policyBuilder;

    public BobbyDispatcher(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
      _policyBuilder = Policy.Handle<Exception>();
    }

    public async Task Handle(RunExportCommand startExport)
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

      var documentBuilderGroups = documentBuilders.GroupBy(builder => builder.DocumentId).ToArray();

      var expectedDocuments = documentBuilderGroups.Select(group => group.Key).ToArray();

      await Emit(new ExportStarted(startExport.CorrelationID, expectedDocuments, startExport.StreamId));

      foreach (var documentBuilderGroup in documentBuilderGroups)
      {

        await Emit(new DocumentBuildRequested(
          startExport.CorrelationID,
          startExport.StreamId,
          startExport.TopicId,
          documentBuilderGroup.Key,
          documentBuilderGroup.Select(documentBuider => (documentBuider.Url, documentBuider.HeadingUrl)).ToArray()
         ));

      }

    }
  }

}
