using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Anabasis.Common
{
  public class AnabasisDocument: Document
  {

    public DocumentItem[] DocumentItems { get; set; }

    public override bool Equals(object obj)
    {
      return obj is AnabasisDocument document &&
             Id == document.Id &&
             Title == document.Title &&
             DocumentItems.All(documentItem => document.DocumentItems.Contains(documentItem));
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(Id, Title, DocumentItems.Select(document=> $"{document.GetHashCode()}")
                                                      .Aggregate((document1, document2) => $"{document1}{document2}")
                                                      .GetHashCode());
    }
  }
}
