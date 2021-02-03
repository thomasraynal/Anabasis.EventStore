using Anabasis.Common.Actor;
using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Bobby
{
  public class BobbyImporter : BaseActor
  {
    public BobbyImporter(SimpleMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => throw new NotImplementedException();

    protected override Task Handle(IEvent @event)
    {
      throw new NotImplementedException();
    }
  }
}
