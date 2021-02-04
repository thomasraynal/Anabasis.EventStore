using Anabasis.Common;
using HtmlAgilityPack;
using Polly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Anabasis.Exporter.Bobby
{
  public class QuotesBuilder
  {
    private readonly PolicyBuilder _policyBuilder;

    public QuotesBuilder(string url, string headingUrl)
    {
      Url = url;

      _policyBuilder = Policy.Handle<Exception>();

      var tags = Regex.Matches(url, "(?<!\\?.+)(?<=\\/)[\\w-]+(?=[/\r\n?]|$)");
      var headings = Regex.Matches(headingUrl, "(?<!\\?.+)(?<=\\/)[\\w-]+(?=[/\r\n?]|$)");

      if (!tags.Any() || !headings.Any()) throw new Exception("untaged");

      Heading = headings.Last().Value;
      Tags = tags.Last().Value;

    }

    public string Heading { get; }
    public string Tags { get; }
    public string Url { get; }
    public string Text { get; private set; }
    public List<Quote> Quotes { get; private set; } = new List<Quote>();

    public string GetAuthor(Match quote)
    {
      var authorMaxSpan = 500;

      if (quote.Index + quote.Length + authorMaxSpan >= Text.Length)
      {
        authorMaxSpan = Text.Length - (quote.Index + quote.Length);
      }

      var authorPredicate = Text.Substring((quote.Index + quote.Length), authorMaxSpan);
      var author = Regex.Match(authorPredicate, "(?<=\\().*?(?=\\))");

      if (author.Success)
      {
        return author.Value.Trim();
      }
      else
      {
        return "(unknown)";
      }

    }

    public void Build()
    {

      var parser = new HtmlWeb();

      var retryPolicy = _policyBuilder.WaitAndRetry(5, (_) => TimeSpan.FromSeconds(1));

      var doc = retryPolicy.Execute(() =>
      {
        return parser.Load(Url);
      });

      Quotes.Clear();

      foreach (var node in doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']").Elements("p"))
      {
        if (!string.IsNullOrEmpty(node.InnerText))
        {
          Text += HtmlEntity.DeEntitize(node.InnerText);
        }
      }

      var quotes = Regex.Matches(Text, "(?<=«).*?(?=»)");

      foreach (Match match in quotes)
      {

        var quote = new Quote
        {
          Text = match.Value.Trim(),
          Tag = Tags.Trim(),
          Author = GetAuthor(match),
        };

        quote.Id = StringExtensions.Md5(quote.Author, quote.Text, quote.Tag);

        Quotes.Add(quote);

      }


      var anabasisDocument = new AnabasisDocument()
      {
        Id = Tags.GetReadableId(),
        Title = Tags,
        DocumentItems = parser.Quotes.Select(quote => new DocumentItem()
        {
          Content = quote.Text,
          Id = quote.Id,
          DocumentId = documentId,
          SecondaryTitleId = quote.Tag,
          // MainTitleId = quote.

        }).ToArray()

      };


    }



    public override bool Equals(object obj)
    {
      var parser = obj as QuotesBuilder;
      return parser != null &&
             Url == parser.Url;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Url);
    }
  }

}
