using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Anabasis.Common
{
  public class AnabasisDocument
  {
    public string Id { get; set; }
    public string Title { get; set; }
    public string Author { get; set; }
    public string Tag { get; set; }

    public AnabasisDocumentItem[] DocumentItems { get; set; }

    public override bool Equals(object obj)
    {
      return obj is AnabasisDocument document &&
             Id == document.Id &&
             Title == document.Title &&
             Author == document.Author &&
             Tag == document.Tag &&
             DocumentItems.All(documentItem => document.DocumentItems.Contains(documentItem));
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Id, Title, Author, Tag, DocumentItems.Select(document=> $"{document.GetHashCode()}")
                                                      .Aggregate((document1, document2) => $"{document1}{document2}")
                                                      .GetHashCode());
    }
  }
}
