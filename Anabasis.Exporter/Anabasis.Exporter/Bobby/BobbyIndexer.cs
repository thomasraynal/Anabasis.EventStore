using Anabasis.Common;
using Anabasis.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Exporter.Bobby
{
  public class BobbyIndexer : BaseIndexer
  {
    public BobbyIndexer(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.Bobby;

  }

}
