using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{
  public class GoogleDocIndexer : BaseIndexer
  {
    public GoogleDocIndexer(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.GoogleDoc;

  }

}
