using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using Lamar;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Actor
{
  public abstract class BaseActor : IActor
  {

    protected BaseActor(IMediator simpleMediator)
    {
      Mediator = simpleMediator;
    }

    public IMediator Mediator { get; }

    public abstract string StreamId { get; }

  }
}
