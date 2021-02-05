using Anabasis.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Exporter.Bobby
{
  public class BobbyIndexer : GoogleDocIndexer
  {
    public BobbyIndexer(IMediator simpleMediator) : base(simpleMediator)
    {
    }
  }
}
