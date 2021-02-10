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

    class ActorConfiguration
    {
      public bool AlwaysConsume { get; set; }
      public IActor Actor{ get; set; }
    }

    class EventConfiguration
    {
      public bool IsSingleConsummer { get; set; }
      public bool IsCommandResponse { get; set; }
      public bool AlwaysConsume { get; set; }
    }

    private readonly ActorConfiguration[] _actors;
    private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
    private readonly ConcurrentDictionary<Type, EventConfiguration> _eventConfiguration;
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<ICommandResponse>> _pendingCommands;

    public SimpleMediator(Container container)
    {

      _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
      _pendingCommands = new ConcurrentDictionary<Guid, TaskCompletionSource<ICommandResponse>>();

      container.Configure(services =>
      {
        services.AddSingleton<IMediator>(this);
        services.AddSingleton(_messageHandlerInvokerCache);
      });

      _eventConfiguration = new ConcurrentDictionary<Type, EventConfiguration>();

      _actors = container.Model.AllInstances
                                .Where(instance => instance.ServiceType.Equals(typeof(IActor)))
                                .SelectMany(type =>
                                {
                                  var inMemoryInstanceAttribute = type.ImplementationType.GetCustomAttributes(typeof(InMemoryInstanceAttribute), true).FirstOrDefault();

                                  var requiredInstanceCount = 1;

                                  if (null != inMemoryInstanceAttribute)
                                  {
                                    requiredInstanceCount = ((InMemoryInstanceAttribute)inMemoryInstanceAttribute).InstanceCount;
                                  }

                                  return Enumerable.Range(0, requiredInstanceCount).Select(_ =>
                                  {
                                    return new ActorConfiguration()
                                    {
                                      Actor = (IActor)container.GetInstance(type.ImplementationType),
                                      AlwaysConsume = type.ImplementationType.GetCustomAttributes(typeof(AlwaysConsume), true).Any()
                                    };

                                  });

                                }).ToArray();


    }

    public Task Send<TCommandResult>(ICommand command, TimeSpan? timeout)
      where TCommandResult : ICommandResponse
    {

      var taskSource = new TaskCompletionSource<ICommandResponse>();

      var cancellationTokenSource = null != timeout ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

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
          IsCommandResponse = key.GetInterfaces().Contains(typeof(ICommandResponse)),
          IsSingleConsummer = key.GetCustomAttributes(typeof(SingleConsumer), true).Any(),
        };

        return eventConfiguration;
      });


      if (eventConfiguration.IsCommandResponse)
      {

        var commandResponse = (ICommandResponse)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Event), message.EventType);

        if (!_pendingCommands.ContainsKey(commandResponse.CommandId)) return Task.CompletedTask;

        var task = _pendingCommands[commandResponse.CommandId];

        task.SetResult(commandResponse);

        _pendingCommands.Remove(commandResponse.EventID, out _);

      }

      else if (eventConfiguration.IsSingleConsummer)
      {
        var candidateConsumerGroups = _actors.Where(actor => actor.Actor.CanConsume(message) && (actor.Actor.StreamId == message.StreamId || actor.Actor.StreamId == StreamIds.AllStream))
                                             .GroupBy(actor => actor.AlwaysConsume);

        foreach (var candidateConsumer in candidateConsumerGroups)
        {
          if (candidateConsumer.Key)
          {
            Parallel.ForEach(candidateConsumer, (actorDescriptor) =>
            {
              Task.Run(() => actorDescriptor.Actor.Enqueue(message));
            });

          }
          else
          {
            var consumer = candidateConsumer.FirstOrDefault();

            if (null != consumer)
            {
              Task.Run(() => consumer.Actor.Enqueue(message));
            }
          }
        }


      }

      else
      {
        Parallel.ForEach(_actors.Where(actor => actor.Actor.StreamId == message.StreamId || actor.Actor.StreamId == StreamIds.AllStream), (actor) =>
        {
          Task.Run(() => actor.Actor.Enqueue(message));

        });
      }


      return Task.CompletedTask;
    }
  }
}
