using Anabasis.Common.Actor;
using Anabasis.Common.Infrastructure;
using Lamar;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{
  public class SimpleMediator : DispatchQueue<Message>, IMediator
  {
    private static IReadOnlyList<IActor> _allActors;
    private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
    private readonly Container _container;

    public SimpleMediator(Container container)
    {
      _container = container;
      _messageHandlerInvokerCache = new MessageHandlerInvokerCache();

      container.Configure(services =>
      {
        services.AddSingleton<IMediator>(this);
        services.AddSingleton(_messageHandlerInvokerCache);
      });

      _allActors = _container.GetAllInstances<IActor>();
    }

    public void Emit(IEvent @event)
    {
      var serializedEvent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

      var message = new Message(@event.StreamId, serializedEvent, @event.GetType());

      Push(message);
    }

    public override Task OnMessageReceived(Message message)
    {
      var @event = (IEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Event), message.EventType);

      Parallel.ForEach(_allActors.Where(actor => actor.StreamId == message.StreamId || actor.StreamId == StreamIds.AllStream), (actor) =>
      {
        Task.Run(() => actor.OnMessageReceived(message));
      });

      return Task.CompletedTask;
    }
  }
}
