using Anabasis.Common;
using Anabasis.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Exporter.Illiade
{
  public class IlliadIndexer : BaseIndexer
  {
    public IlliadIndexer(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.Illiad;

  }

}
