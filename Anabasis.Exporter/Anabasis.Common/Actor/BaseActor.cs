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
    protected BaseActor(SimpleMediator simpleMediator)
    {
      Mediator = simpleMediator;
    }

    public SimpleMediator Mediator { get; }

    public void Emit(IEvent @event)
    {
      var serializedEvent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

      var message = new Message(serializedEvent, @event.GetType());

      Mediator.Send(message);
    }

    protected abstract Task Handle(IEvent @event);

    public async Task Handle(Message message)
    {
      var @event = (IEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Event), message.EventType);

      await Handle(@event);
    }
  }
}
