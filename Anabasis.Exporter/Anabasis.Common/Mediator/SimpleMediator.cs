using Anabasis.Common.Actor;
using Anabasis.Common.Infrastructure;
using Lamar;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{
  public class SimpleMediator : IMediator, IDisposable
  {
    private static IReadOnlyList<IActor> _allActors;
    private readonly Task _workProc;
    private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
    private readonly Container _container;
    private readonly BlockingCollection<Message> _workQueue;

    public SimpleMediator(Container container)
    {
      _container = container;
      _workQueue = new BlockingCollection<Message>();
      _workProc = Task.Run(HandleWork, CancellationToken.None);

      _messageHandlerInvokerCache = new MessageHandlerInvokerCache();

      container.Configure(services =>
      {
        services.AddSingleton<IMediator>(this);
      });

      _allActors = _container.GetAllInstances<IActor>();
    }

    public void Send(Message message)
    {
      _workQueue.Add(message);
    }

    private void HandleWork()
    {

      foreach (var message in _workQueue.GetConsumingEnumerable())
      {
        var @event = (IEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Event), message.EventType);

        Parallel.ForEach(_allActors.Where(actor => actor.StreamId == message.StreamId || actor.StreamId == StreamIds.AllStream), (actor) =>
           {
             var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(actor.GetType(), message.EventType);

             if (null != candidateHandler)
             {
               Task.Run(() => (Task)candidateHandler.Invoke(actor, new object[] { @event }));
             }

           });

      }
    }

    public void Emit(IEvent @event)
    {
      var serializedEvent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

      var message = new Message(@event.StreamId, serializedEvent, @event.GetType());

      Send(message);
    }

    public void Dispose()
    {
      _workQueue.Dispose();
      _workProc.Dispose();
    }
  }
}
