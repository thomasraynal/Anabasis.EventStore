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
  public abstract class BaseActor : DispatchQueue<Message>, IActor
  {

    protected BaseActor(IMediator simpleMediator)
    {
      
      Mediator = simpleMediator;
      ActorId = $"{GetType()}-{Guid.NewGuid()}";

      _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
    }

    public IMediator Mediator { get; }

    private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;

    public abstract string StreamId { get; }

    public string ActorId { get; }

    protected async override Task OnMessageReceived(Message message)
    {
      var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), message.EventType);

      if (null != candidateHandler)
      {
        var @event = (IEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Event), message.EventType);

        await (Task)candidateHandler.Invoke(this, new object[] { @event });

      }
    }
    
    public bool CanConsume(Message message)
    {
      return null != _messageHandlerInvokerCache.GetMethodInfo(GetType(), message.EventType);
    }

    public override bool Equals(object obj)
    {
      return obj is BaseActor actor &&
             ActorId == actor.ActorId;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(ActorId);
    }
  }
}
