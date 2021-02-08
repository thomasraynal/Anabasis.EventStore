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

      _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
    }

    public IMediator Mediator { get; }

    private MessageHandlerInvokerCache _messageHandlerInvokerCache;

    public abstract string StreamId { get; }

    public override async Task OnMessageReceived(Message message)
    {
      var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), message.EventType);

      if (null != candidateHandler)
      {
        var @event = (IEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Event), message.EventType);

        await (Task)candidateHandler.Invoke(this, new object[] { @event });

      }
    }
  }
}
