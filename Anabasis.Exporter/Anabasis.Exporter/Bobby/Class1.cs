using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Bobby
{
  public class Path
  {
    public Path(string url)
    {
      Url = url;
    }

    public string Url { get; private set; }
    public string Tag
    {
      get
      {

        if (Program.IsTest)
        {
          return System.IO.Path.GetFileName(Url);
        }

        var matches = Regex.Matches(Url, "(?<!\\?.+)(?<=\\/)[\\w-]+(?=[/\r\n?]|$)");

        if (!matches.Any()) throw new Exception("untaged");

        return matches.Last().Value;
      }
    }
  }

  public class QuotationParsingContext
  {

    public void AddUrls(params Path[] paths)
    {
      foreach (var path in paths)
      {
        try
        {
          var context = new QuotationParser(path);

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

    public IEnumerable<Quotation> Quotations()
    {
      return QuotationParsers.SelectMany(p => p.Quotations);
    }

    public void Build()
    {
      Parallel.ForEach(QuotationParsers, new ParallelOptions() { MaxDegreeOfParallelism = 4 }, (parser) =>
      {
        try
        {
          parser.Build();

        //  Program.Logger.Information($"Built [{parser.Path.Url}]");

        }
        catch (Exception)
        {
      //    Program.Logger.Error($"Unable to create [{parser.Path.Url}]");
        }

      });

    }

    private List<QuotationParser> QuotationParsers { get; set; } = new List<QuotationParser>();
  }

  public class QuotationParser
  {
    private Path _path;

    public QuotationParser(Path path)
    {
      Path = path;
    }

    public Path Path
    {
      get
      {
        return _path;
      }
      private set
      {
        _path = value;

        if (Program.IsTest)
        {
          Text = File.ReadAllText(_path.Url);
        }
        else
        {

          if (!String.IsNullOrEmpty(_path.Url))
          {
            var parser = new HtmlWeb();
            var doc = parser.Load(_path.Url);

            foreach (var node in doc.DocumentNode.SelectSingleNode("//div[@class='entry-content']").Elements("p"))
            {
              if (!String.IsNullOrEmpty(node.InnerText))
              {
                Text += HtmlEntity.DeEntitize(node.InnerText);
              }
            }

            Directory.CreateDirectory("./data");

            File.WriteAllText($"./data/{_path.Tag}", Text, Encoding.Unicode);
          }

        }
      }
    }

    public string Text { get; private set; } = string.Empty;

    public void Build()
    {
      Quotations.Clear();

      var quotes = Regex.Matches(Text, "(?<=«).*?(?=»)");

      foreach (Match quote in quotes)
      {
        var author = GetAuthor(quote);

        if (author == string.Empty)
        {
       //   Program.Logger.Warning($"Unable to find author for quote [{Path.Tag}] [{new String(quote.Value.Take(quote.Value.Count() > 200 ? 200 : quote.Value.Count()).ToArray())}]");
        }





        var q = new Quotation
        {
          Quote = quote.Value.Trim(),
          Tag = Path.Tag.Trim(),
          Author = GetAuthor(quote),

        };

        var crypt = new SHA256Managed();
        var hash = String.Empty;
        var crypto = crypt.ComputeHash(Encoding.ASCII.GetBytes(q.Author + q.Quote + q.Tag));
        foreach (var b in crypto)
        {
          hash += b.ToString("x2");
        }

        q.Id = hash;

        Quotations.Add(q);

       // Program.Logger.Information($"Parsed [{Path.Url}]");
      }
    }

    public List<Quotation> Quotations { get; private set; } = new List<Quotation>();

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

    public override bool Equals(object obj)
    {
      var parser = obj as QuotationParser;
      return parser != null &&
             Path == parser.Path;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Path);
    }
  }

  //[BsonIgnoreExtraElements]
  public class Quotation : IEquatable<Quotation>
  {
    // [BsonId]
    public String Id { get; set; }

    // [BsonElement]
    public String Quote { get; set; }
    //[BsonElement]
    public String Author { get; set; }
    //public String Comment { get; set; }
    //[BsonElement]
    public String Tag { get; set; }

    public List<String> Tags
    {
      get
      {
        return Tag.Split("-").ToList();
      }
    }

    public string ToDelimited()
    {
      return $"{Tag}||{Author}||{Quote}||{Id}";
    }

    public override bool Equals(object obj)
    {
      if (!(obj is Quotation)) return false;
      return (obj as Quotation).GetHashCode() == base.GetHashCode();
    }

    public override int GetHashCode()
    {
      return Id.GetHashCode();
    }

    public bool Equals(Quotation other)
    {
      return other.Id == Id;
    }
  }


  public class Importer
  {
    public async Task Import(List<Quotation> quotations)
    {
      //var mongoClient = new MongoClient("mongodb://127.0.0.1:27017");
      //var db = mongoClient.GetDatabase("bobbymedit");
      //var collection = db.GetCollection<Quotation>("quotations");
      //var queryable = db.GetCollection<Quotation>("quotations").AsQueryable();

      //var ids = queryable.Select(q => q.Id).ToList();

      //var candidates = quotations.Where(q => !ids.Contains(q.Id)).ToList();

      //foreach (var quotation in candidates)
      //{
      //  try
      //  {
      //    Program.Logger.Information($"Importing {quotation.Id}");
      //    await collection.InsertOneAsync(quotation);
      //  }
      //  catch (Exception ex)
      //  {
      //    Program.Logger.Error(ex.ToString());
      //  }

      //}

    }
  }

  class Program
  {
   // internal static Logger Logger;
    internal static bool IsTest;

    static IEnumerable<Path> GetUrls()
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

    static void Main(string[] args)
    {
      IsTest = false;

      //var configuration = new ConfigurationBuilder()
      //        .AddJsonFile("appsettings.json")
      //        .Build();

      //Logger = new LoggerConfiguration()
      //    .ReadFrom.Configuration(configuration)
      //    .CreateLogger();

      var context = new QuotationParsingContext();

      if (IsTest)
      {
        var files = Directory.GetFiles("./data")
                             .Select(f => new Path(f)).ToArray();

        context.AddUrls(files);
      }
      else
      {
        var urls = GetUrls().Distinct().ToArray();
        context.AddUrls(urls);
      }



      context.Build();

      var quotations = context.Quotations().Distinct().ToList();

      File.WriteAllLines($"data_{DateTime.Now.ToString("MM-dd-yyyy HH-mm-ss")}.csv", quotations.Select(q => q.ToDelimited()).ToList(), Encoding.Unicode);

      //var importer = new Importer();
      //importer.Import(quotations).Wait();

    }
  }
}
