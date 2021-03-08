using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
  public class AnabasisDocumentIndex
  {
    public string Id { get; set; }
    public string Title { get; set; }

    public AnabasisDocumentIndex[] DocumentIndices { get; set; }

  }
}
