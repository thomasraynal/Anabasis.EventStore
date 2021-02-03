using Anabasis.Common.Actor;
using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Bobby
{
  public class BobbyExporter : BaseActor
  {
    public BobbyExporter(SimpleMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => throw new NotImplementedException();

    private IEnumerable<Path> GetUrls()
    {
      var index = "http://bobbymedit.fr/table-des-matieres/";

      var parser = new HtmlWeb();
      var doc = parser.Load(index);

      foreach (var node in doc.DocumentNode.SelectNodes("//a"))
      {
        var href = node.Attributes["href"];

        if (null != href)
        {
          yield return new Path(href.Value);
        }
      }
    }

    protected override Task Handle(IEvent @event)
    {
      var context = new QuotationParsingContext();

      var urls = GetUrls().Distinct().ToArray();
      context.AddUrls(urls);

      context.Build();

      var quotations = context.Quotations().Distinct().ToList();

      return Task.CompletedTask;

      // File.WriteAllLines($"data_{DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss")}.csv", quotations.Select(q => q.ToDelimited()).ToList(), Encoding.Unicode);

      //var importer = new Importer();
      //importer.Import(quotations).Wait();
    }
  }
}
