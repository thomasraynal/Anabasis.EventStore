using Anabasis.Common;
using HtmlAgilityPack;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Anabasis.Exporter.Bobby
{
  public class BobbyDocumentBuilder
  {
    private readonly PolicyBuilder _policyBuilder;

    public BobbyDocumentBuilder(string url, string headingUrl)
    {
      Url = url;

      _policyBuilder = Policy.Handle<Exception>();

      var tags = Regex.Matches(url, "(?<!\\?.+)(?<=\\/)[\\w-]+(?=[/\r\n?]|$)");
      var headings = Regex.Matches(headingUrl, "(?<!\\?.+)(?<=\\/)[\\w-]+(?=[/\r\n?]|$)");

      if (!tags.Any() || !headings.Any()) throw new Exception("untaged");

      DocumentId = headings.Last().Value;
      MainTitle = tags.Last().Value;
      HeadingUrl = headingUrl;
    }

    public string HeadingUrl { get; }

    public string DocumentId { get; }
    public string MainTitle { get; }
    public string Url { get; }
    public string Text { get; private set; }

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

    public AnabasisDocumentItem[] BuildItems(AnabasisDocument anabasisDocument)
    {

      var parser = new HtmlWeb();

      var quotes = new List<BobbyQuote>();

      var retryPolicy = _policyBuilder.WaitAndRetry(5, (_) => TimeSpan.FromSeconds(10));

      var htmlDocument = retryPolicy.Execute(() =>
      {
        return parser.Load(Url);
      });


      foreach (var node in htmlDocument.DocumentNode.SelectSingleNode("//div[@class='entry-content']").Elements("p"))
      {
        if (!string.IsNullOrEmpty(node.InnerText))
        {
          Text += HtmlEntity.DeEntitize(node.InnerText);
        }
      }

      var allMatch = Regex.Matches(Text, "(?<=«).*?(?=»)");

      foreach (Match match in allMatch)
      {

        var quote = new BobbyQuote
        {
          Text = match.Value.Trim(),
          Tag = MainTitle.Trim(),
          Author = GetAuthor(match),
        };

        quote.Id = StringExtensions.Md5(quote.Author, quote.Text, quote.Tag);

        quotes.Add(quote);

      }

      var mainTitleId = MainTitle.GetReadableId();

      var documentPosition = 0;

      var mainTitle = new AnabasisDocumentItem()
      {
        Id = mainTitleId,
        IsMainTitle = true,
        Content = MainTitle,
        DocumentId = anabasisDocument.Id,
        ParentId = anabasisDocument.Id,
        Position = documentPosition

      };

      var anabasisDocumentItem = quotes.Select(quote => new AnabasisDocumentItem()
      {
        Content = $"{quote.Text} ({quote.Author})",
        Id = quote.Id,
        DocumentId = anabasisDocument.Id,
        ParentId = mainTitleId,
        MainTitleId = mainTitleId,
        Position = ++documentPosition

      }).ToArray();

      return new[] { mainTitle }.Concat(anabasisDocumentItem).ToArray();

    }

    public override bool Equals(object obj)
    {
      var parser = obj as BobbyDocumentBuilder;
      return parser != null &&
             Url == parser.Url;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Url);
    }
  }

}
