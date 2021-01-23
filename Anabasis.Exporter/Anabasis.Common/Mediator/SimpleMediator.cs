using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Infrastructure;
using Lamar;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{
  public class SimpleMediator : IMediator, IDisposable
  {
    private static IReadOnlyList<IActor> _allActors;
    private readonly Task _workProc;
    private readonly Container _container;
    private readonly BlockingCollection<Message> _workQueue;

    public SimpleMediator(Container container)
    {
      _container = container;
      _workQueue = new BlockingCollection<Message>();
      _workProc = Task.Run(HandleWork, CancellationToken.None);

      container.Configure(services =>
      {
        services.AddSingleton(this);
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
        Parallel.ForEach(_allActors, async (actor) =>
         {
           await actor.Handle(message);

         });

      }
    }

    public void Emit(IEvent @event)
    {
      var serializedEvent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

      var message = new Message(serializedEvent, @event.GetType());

      Send(message);
    }

    public void Dispose()
    {
      _workQueue.Dispose();
      _workProc.Dispose();
    }
  }
}
