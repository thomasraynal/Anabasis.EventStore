using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Tests.POC
{
  public class AnabasisDocumentIndex2
  {
    public string Id { get; set; }
    public string Title { get; set; }

    public AnabasisDocumentIndex2[] DocumentIndices { get; set; }

  }
}
