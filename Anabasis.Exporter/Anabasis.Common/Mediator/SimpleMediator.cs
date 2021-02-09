using Anabasis.Common.Actor;
using Anabasis.Common.Events;
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

  //todo: should be actor
  public class SimpleMediator : DispatchQueue<Message>, IMediator
  {

    class EventConfiguration
    {
      public bool IsSingleConsummer { get; set; }
      public bool IsCommandResponse { get; set; }
    }

    private readonly IActor[] _allActors;
    private readonly string _simpleMediatorId;
    private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
    private readonly ConcurrentDictionary<Type, EventConfiguration> _eventConfiguration;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ICommandResponse>> _pendingCommands;

    public SimpleMediator(Container container)
    {

      _simpleMediatorId = $"{nameof(SimpleMediator)}{Guid.NewGuid()}";

      _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
      _pendingCommands = new ConcurrentDictionary<Guid, TaskCompletionSource<ICommandResponse>>();

      container.Configure(services =>
      {
        services.AddSingleton<IMediator>(this);
        services.AddSingleton(_messageHandlerInvokerCache);
      });

      _eventConfiguration = new ConcurrentDictionary<Type, EventConfiguration>();

      _allActors = container.Model.AllInstances
                                   .Where(instance => instance.ServiceType.Equals(typeof(IActor)))
                                   .SelectMany(type =>
                                   {
                                     var inMemoryInstanceAttribute = type.ImplementationType.GetCustomAttributes(typeof(InMemoryInstanceAttribute), true).FirstOrDefault();

                                     var requiredInstanceCount = 1;

                                     if (null != inMemoryInstanceAttribute)
                                     {
                                       requiredInstanceCount = ((InMemoryInstanceAttribute)inMemoryInstanceAttribute).InstanceCount;
                                     }

                                     return Enumerable.Range(0, requiredInstanceCount).Select(_ => (IActor)container.GetInstance(type.ImplementationType));

                                   }).ToArray();


    }

    public Task Send<TCommand, TCommandResult>(TCommand command, TimeSpan? timeout)
      where TCommand : BaseCommand // use a wrapper
      where TCommandResult : ICommandResponse
    {

      command.CallerId = _simpleMediatorId;

      var taskSource = new TaskCompletionSource<ICommandResponse>();

      var cancellationTokenSource = null == timeout ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

      cancellationTokenSource.Token.Register(() => taskSource.TrySetCanceled(), false);

      _pendingCommands[command.EventID] = taskSource;

      Emit(command);

      return taskSource.Task.ContinueWith(task =>
      {
        if (task.IsCompletedSuccessfully) return (TCommandResult)task.Result;
        if (task.IsCanceled) throw new Exception("timeout");

        throw task.Exception;

      }, cancellationTokenSource.Token);

    }

    public void Emit(IEvent @event)
    {
      var serializedEvent = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));

      var message = new Message(@event.StreamId, serializedEvent, @event.GetType());

      Enqueue(message);
    }

    protected override Task OnMessageReceived(Message message)
    {

      var eventConfiguration = _eventConfiguration.GetOrAdd(message.EventType, (key) =>
      {
        var eventConfiguration = new EventConfiguration()
        {
          IsCommandResponse = key.GetCustomAttributes(typeof(ICommandResponse), true).Any(),
          IsSingleConsummer = key.GetCustomAttributes(typeof(SingleConsumer), true).Any()
        };

        return eventConfiguration;
      });


      if (eventConfiguration.IsCommandResponse)
      {
        if (_simpleMediatorId == message.CallerId)
        {
          var commandResponse = (ICommandResponse)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Event), message.EventType);

          var task = _pendingCommands[commandResponse.CommandId];

          task.SetResult(commandResponse);

          _pendingCommands.Remove(commandResponse.CommandId, out _);

        }

        var consumer = _allActors.FirstOrDefault(actor => actor.ActorId == message.CallerId);

        if (null == consumer) throw new InvalidOperationException("Caller not found");

        consumer.Enqueue(message);

      }

      else if (eventConfiguration.IsSingleConsummer)
      {
        var consumer = _allActors.FirstOrDefault(actor => actor.CanConsume(message));

        if (null != consumer)
        {
          consumer.Enqueue(message);
        }
      }

      else
      {
        Parallel.ForEach(_allActors.Where(actor => actor.StreamId == message.StreamId || actor.StreamId == StreamIds.AllStream), (actor) =>
        {
          Task.Run(() => actor.Enqueue(message));

        });
      }


      return Task.CompletedTask;
    }
  }
}
