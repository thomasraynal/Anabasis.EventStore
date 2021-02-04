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
    private const string quotesIndex = "http://bobbymedit.fr/table-des-matieres/";

    public BobbyExporter(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.Bobby;

    public Task Handle(StartExport startExport)
    {

      var htmlWeb = new HtmlWeb();
      var quotationParsers = new List<QuotesBuilder>();

      var doc = htmlWeb.Load(quotesIndex);

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
              var quoteParser = new QuotesBuilder(href.Value, currentHeading);

              if (quotationParsers.Contains(quoteParser))
              {
                quotationParsers.Remove(quoteParser);
              }

              quotationParsers.Add(quoteParser);
            }

          }

          catch (Exception)
          {
          }
        }

      }

      //Parallel.ForEach(quotationParsers, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (parser) =>
      //{

      foreach (var parser in quotationParsers)
      {

        try
        {
          parser.Build();

          var documentId = StringExtensions.Md5(parser.Url);

          var anabasisDocument = new AnabasisDocument()
          {
            Id = documentId,
            Title = parser.Tags,
            DocumentItems = parser.Quotes.Select(quote => new DocumentItem()
            {
              Content = quote.Text,
              Id = quote.Id,
              DocumentId = documentId,
              SecondaryTitleId = quote.Tag,
              // MainTitleId = quote.

            }).ToArray()

          };

          Mediator.Emit(new DocumentDefined(startExport.CorrelationID, startExport.StreamId, startExport.TopicId, $"{parser.Url}"));

        }
        catch (Exception)
        {
          Mediator.Emit(new DocumentCreationFailed(startExport.CorrelationID, startExport.StreamId, startExport.TopicId, $"{parser.Url}"));
        }

      }

      //});

      return Task.CompletedTask;

    }

  }
}
