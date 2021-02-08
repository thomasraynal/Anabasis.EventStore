using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Anabasis.Exporter.Illiade
{
  [InMemoryInstance(1)]
  public class IlliadExporter : BaseActor
  {
    private const string quotesIndex = "https://citations.institut-iliade.com/plan-du-site/";

    public IlliadExporter(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.Illiad;

    public Task Handle(StartExport startExport)
    {

      var htmlWeb = new HtmlWeb();
      var documentBuilders = new List<IlliadDocumentBuilder>();

      var doc = htmlWeb.Load(quotesIndex);
 
      var nodes = doc.DocumentNode.SelectNodes("//*[@class='authors-list-item-title']/a")
                                  .Select(node =>
                                  {
                                    var title = node.InnerText.Clean();

                                    return (title, id : title.GetReadableId(), url: node.Attributes["href"].Value);

                                  }).ToArray();


      Mediator.Emit(new ExportStarted(startExport.CorrelationID, nodes.Select(node=>node.id).ToArray(), startExport.StreamId, startExport.TopicId));

      foreach (var (title, id, url) in nodes)
      {


        var documentBuilder = new IlliadDocumentBuilder(title, id, url);

        documentBuilders.Add(documentBuilder);

        var anabasisDocument = documentBuilder.BuildDocument();

        Mediator.Emit(new DocumentCreated(startExport.CorrelationID, startExport.StreamId, startExport.TopicId, anabasisDocument));


      }



      Mediator.Emit(new ExportEnded(startExport.CorrelationID, startExport.StreamId, startExport.TopicId));

      return Task.CompletedTask;

    }
  }
}
