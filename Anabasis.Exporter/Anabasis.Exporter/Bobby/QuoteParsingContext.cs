using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Bobby
{
  public class QuoteParsingContext
  {

    private const string quotesIndex = "http://bobbymedit.fr/table-des-matieres/";

    public IEnumerable<Quote> Quotations()
    {
      return QuotationParsers.SelectMany(parser => parser.Quotations);
    }

    public void Build()
    {

      var parser = new HtmlWeb();
      var doc = parser.Load(quotesIndex);

      foreach (var node in doc.DocumentNode.SelectNodes("//a"))
      {
        var href = node.Attributes["href"];

        if (null != href)
        {

          try
          {
            var context = new QuoteParser(href.Value);

            if (QuotationParsers.Contains(context))
            {
              QuotationParsers.Remove(context);
            }

            QuotationParsers.Add(context);

            //   Program.Logger.Information($"Created [{path.Url}]");
          }
          catch (Exception)
          {
            //    Program.Logger.Error($"Unable to handle url [{path.Url}]");
          }
        }
      }

      foreach(var quotationParser in QuotationParsers)
      {
        quotationParser.Build();
      }

      //Parallel.ForEach(QuotationParsers, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (parser) =>
      //{
      //  try
      //  {
      //    parser.Build();

      //    //  Program.Logger.Information($"Built [{parser.Path.Url}]");

      //  }
      //  catch (Exception)
      //  {
      //    //    Program.Logger.Error($"Unable to create [{parser.Path.Url}]");
      //  }

      //});

    }

    private List<QuoteParser> QuotationParsers { get; set; } = new List<QuoteParser>();
  }
}
