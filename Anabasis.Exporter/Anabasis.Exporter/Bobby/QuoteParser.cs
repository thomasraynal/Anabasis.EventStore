using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Anabasis.Exporter.Bobby
{
  public class QuoteParser
  {

    public QuoteParser(string url)
    {
      Url = url;

      var matches = Regex.Matches(url, "(?<!\\?.+)(?<=\\/)[\\w-]+(?=[/\r\n?]|$)");

      if (!matches.Any()) throw new Exception("untaged");

      Tag = matches.Last().Value;

    }
    
    public string Tag { get; }
    public string Url { get; }
    public string Text { get; private set; } = string.Empty;
    public List<Quote> Quotations { get; private set; } = new List<Quote>();

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

      Quotations = new List<Quote>();

      var parser = new HtmlWeb();
      var doc = parser.Load(Url);

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
        var author = GetAuthor(match);

        if (author == string.Empty)
        {
          //   Program.Logger.Warning($"Unable to find author for quote [{Path.Tag}] [{new String(quote.Value.Take(quote.Value.Count() > 200 ? 200 : quote.Value.Count()).ToArray())}]");
        }

        var quote = new Quote
        {
          Text = match.Value.Trim(),
          Tag = Tag.Trim(),
          Author = GetAuthor(match),

        };

        var crypt = new SHA256Managed();
        var hash = string.Empty;
        var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(quote.Author + quote.Text + quote.Tag));

        foreach (var b in crypto)
        {
          hash += b.ToString("x2");
        }

        quote.Id = hash;

        Quotations.Add(quote);

        // Program.Logger.Information($"Parsed [{Path.Url}]");
      }
    }



    public override bool Equals(object obj)
    {
      var parser = obj as QuoteParser;
      return parser != null &&
             Url == parser.Url;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Url);
    }
  }

}
