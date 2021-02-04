using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
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

    public override string StreamId => StreamIds.Bobby;

    public Task Handle(StartExport startExport)
    {
      var context = new QuoteParsingContext();

      context.Build();

      var quotations = context.Quotations().Distinct().ToList();

      return Task.CompletedTask;

      // File.WriteAllLines($"data_{DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss")}.csv", quotations.Select(q => q.ToDelimited()).ToList(), Encoding.Unicode);

      //var importer = new Importer();
      //importer.Import(quotations).Wait();
    }

    protected override Task Handle(IEvent @event)
    {
      throw new NotImplementedException();
    }
  }
}
