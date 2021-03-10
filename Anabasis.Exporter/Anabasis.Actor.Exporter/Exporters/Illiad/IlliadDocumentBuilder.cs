using Anabasis.Common;
using HtmlAgilityPack;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Anabasis.Exporter.Illiad
{
  public class IlliadDocumentBuilder
  {
    private readonly PolicyBuilder _policyBuilder;

    public IlliadDocumentBuilder(string title, string id, string url)
    {
      
      _policyBuilder = Policy.Handle<Exception>();

      Title = title;
      DocumentId = id;
      Url = url;

    }

    public string DocumentId { get; }
    public string Title { get; }
    public string Url { get; }
    public string Text { get; private set; }

    public AnabasisDocument BuildDocument()
    {

      var parser = new HtmlWeb();

      var retryPolicy = _policyBuilder.WaitAndRetry(5, (_) => TimeSpan.FromSeconds(1));

      var htmlDocument = retryPolicy.Execute(() =>
      {
        var document = parser.Load(Url);

        if (!document.DocumentNode.InnerHtml.Contains("ILIADE")) throw new Exception("failure");

        return document;
      });


      var illiadQuotes = new List<IlliadQuote>();
      var documentPosition = 0;

      var anabasisDocument = new AnabasisDocument()
      {
        Id = DocumentId,
        Title = Title
      };

      foreach (var node in htmlDocument.DocumentNode.SelectNodes("//div[@class='post-content-inner et_pb_blog_show_content']"))
      {

        try
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

          illiadQuotes.Add(new IlliadQuote()
          {
            Author = author,
            Content = quote,
            Source = source,
            Id = $"{Guid.NewGuid()}",
            DocumentId = anabasisDocument.Id,
            ParentId = DocumentId,
            Position = ++documentPosition

          });

        }

        catch (Exception)
        {
        }

      }

      anabasisDocument.DocumentItems = illiadQuotes.ToArray();

      return anabasisDocument;

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
