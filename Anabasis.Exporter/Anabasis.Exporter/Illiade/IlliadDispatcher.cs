using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using Anabasis.Exporter.Illiade;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common
{
  public class IlliadDispatcher : BaseActor
  {

    private const string quotesIndex = "https://citations.institut-iliade.com/plan-du-site/";

    public IlliadDispatcher(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.Illiad;

    public Task Handle(StartExportCommand startExport)
    {
      var htmlWeb = new HtmlWeb();
      var documentBuilders = new List<IlliadDocumentBuilder>();

      var doc = htmlWeb.Load(quotesIndex);

      var nodes = doc.DocumentNode.SelectNodes("//*[@class='authors-list-item-title']/a")
                                  .Select(node =>
                                  {
                                    var title = node.InnerText.Clean();

                                    return (title, id: title.GetReadableId(), url: node.Attributes["href"].Value);

                                  }).Take(5).ToArray();

      Mediator.Emit(new ExportStarted(startExport.CorrelationID, nodes.Select(node => node.id).ToArray(), startExport.StreamId, startExport.TopicId));

      foreach (var (documentTitle, documentId, documentUrl) in nodes)
      {

        Mediator.Emit(new ExportDocument(
          startExport.CorrelationID,
          startExport.StreamId,
          startExport.TopicId,
          documentId,
          documentTitle,
          documentUrl));

      }

      return Task.CompletedTask;

    }

  }

}
