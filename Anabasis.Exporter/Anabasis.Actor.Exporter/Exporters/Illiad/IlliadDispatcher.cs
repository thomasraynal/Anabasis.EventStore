using Anabasis.Actor;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Illiad
{
  public class IlliadDispatcher : BaseActor
  {

    private const string quotesIndex = "https://citations.institut-iliade.com/plan-du-site/";

    public IlliadDispatcher(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
    }

    public async Task Handle(StartExportCommand startExport)
    {
      var htmlWeb = new HtmlWeb();
      var documentBuilders = new List<IlliadDocumentBuilder>();

      var doc = htmlWeb.Load(quotesIndex);

      var nodes = doc.DocumentNode.SelectNodes("//*[@class='authors-list-item-title']/a")
                                  .Select(node =>
                                  {
                                    var title = node.InnerText.Clean();

                                    return (title, id: title.GetReadableId(), url: node.Attributes["href"].Value);

                                  }).ToArray();

      await Emit(new ExportStarted(startExport.CorrelationID, nodes.Select(node => node.id).ToArray(), startExport.StreamId, startExport.TopicId));

      foreach (var (documentTitle, documentId, documentUrl) in nodes)
      {

        await Emit(new ExportDocument(
          startExport.CorrelationID,
          startExport.StreamId,
          startExport.TopicId,
          documentId,
          documentTitle,
          documentUrl));

      }

    }

  }

}
