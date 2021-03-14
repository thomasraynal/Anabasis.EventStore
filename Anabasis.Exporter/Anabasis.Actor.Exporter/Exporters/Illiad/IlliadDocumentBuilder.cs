using Anabasis.Common;
using HtmlAgilityPack;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Anabasis.Exporter.Illiad
{
  public class IlliadDocumentBuilder
  {
    private readonly PolicyBuilder _policyBuilder;

    public IlliadDocumentBuilder( string id, string url)
    {

      _policyBuilder = Policy.Handle<Exception>();

      DocumentId = id;
      Url = url;

    }

    public string DocumentId { get; }
    public string Url { get; }
    public string Text { get; private set; }

    public HtmlDocument[] GetPages()
    {

      var results = new List<HtmlDocument>();

      var parser = new HtmlWeb();

      var retryPolicy = _policyBuilder.WaitAndRetry(5, (_) => TimeSpan.FromSeconds(1));

      var htmlDocumentRoot = retryPolicy.Execute(() =>
      {
        var document = parser.Load(Url);

        if (!document.DocumentNode.InnerHtml.Contains("ILIADE")) throw new Exception("failure");

        return document;
      });

      results.Add(htmlDocumentRoot);

      var htmlDocumentNextPageNodes = htmlDocumentRoot.DocumentNode.SelectNodes("//div[@class='wp-pagenavi']/a");

      if (null != htmlDocumentNextPageNodes)
      {
        var htmlDocumentNextPages = htmlDocumentNextPageNodes.Select(node => node.GetAttributeValue("href", "href")).Distinct().ToArray();

        foreach (var nextPage in htmlDocumentNextPages)
        {
          var htmlDocument = retryPolicy.Execute(() =>
          {
            var document = parser.Load(new Uri(nextPage));

            if (!document.DocumentNode.InnerHtml.Contains("ILIADE")) throw new Exception("failure");

            return document;
          });

          results.Add(htmlDocument);
        }
      }


      return results.ToArray();

    }

    public IlliadAnabasisDocument[] Build()
    {
      var illiadAnabasisDocuments = new List<IlliadAnabasisDocument>();

      foreach (var htmlDocument in GetPages())
      {
        var buildDocuments = BuildDocument(htmlDocument);

        illiadAnabasisDocuments.AddRange(buildDocuments);
      }

      return illiadAnabasisDocuments.ToArray();

    }

    private IlliadAnabasisDocument[] BuildDocument(HtmlDocument htmlDocument)
    {

      var illiadAnabasisDocuments = new List<IlliadAnabasisDocument>();

      foreach (var node in htmlDocument.DocumentNode.SelectNodes("//div[@class='post-content-inner et_pb_blog_show_content']"))
      {

        var quoteNodes = node.Elements("p").Where(node => !string.IsNullOrEmpty(node.InnerText)).ToArray();

        var authorNodeIndex = quoteNodes.FirstOrDefault(node => null != node.Element("strong"));

        var quote = HttpUtility.HtmlDecode(quoteNodes.TakeWhile(node => node != authorNodeIndex).Select(node => node.InnerText.Clean()).Aggregate((str1, str2) => $"{str1} {str2}"));

        var authorNode = quoteNodes.ElementAt(1).Element("strong");

        var author = authorNode == null ? null : HttpUtility.HtmlDecode(authorNodeIndex.Element("strong").InnerText.Clean());


        string source = null;

        if (null != authorNode)
        {

          source = HttpUtility.HtmlDecode(authorNodeIndex.InnerText.Clean());

          if (null != author)
          {
            source = source.Replace(author, "").Trim();
          }

        }
        else
        {
          source = quoteNodes.Last().InnerText.Clean();
        }

        illiadAnabasisDocuments.Add(new IlliadAnabasisDocument()
        {
          Author = author,
          Tag = author,
          Content = quote.Replace("« ", "").Replace(" »", ""),
          Source = source,
          Id = $"{Guid.NewGuid()}",
        });

      }


      return illiadAnabasisDocuments.ToArray(); ;

    }

    public override bool Equals(object obj)
    {
      var parser = obj as IlliadDocumentBuilder;
      return parser != null &&
             Url == parser.Url;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Url);
    }
  }
}
