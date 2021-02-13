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
using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{

  //todo: should be actor
  public class SimpleMediator : DispatchQueue<Message>, IMediator
  {

    class ActorConfiguration
    {
      public bool AlwaysConsume { get; set; }
      public IActor Actor { get; set; }
    }

    class EventConfiguration
    {
      public bool IsSingleConsummer { get; set; }
      public bool IsCommandResponse { get; set; }
    }

    private readonly Dictionary<Type, EventConfiguration> _eventConfiguration;
    private readonly ActorConfiguration[] _actors;


    public SimpleMediator(Container container)
    {

      container.Configure(services =>
      {
        services.AddSingleton<IMediator>(this);
      });

      _eventConfiguration = new Dictionary<Type, EventConfiguration>();

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


      var @event = (IEvent)JsonConvert.DeserializeObject(Encoding.UTF8.GetString(message.Event), message.EventType);


      if (eventConfiguration.IsSingleConsummer)
      {
        var candidateConsumerGroups = _actors.Where(actor => actor.Actor.CanConsume(@event) && (actor.Actor.StreamId == message.StreamId || actor.Actor.StreamId == StreamIds.AllStream))
                                             .GroupBy(actor => actor.AlwaysConsume);

        foreach (var candidateConsumer in candidateConsumerGroups)
        {
          if (candidateConsumer.Key)
          {
            Parallel.ForEach(candidateConsumer, (actorDescriptor) =>
            {
              Task.Run(() => actorDescriptor.Actor.Enqueue(@event));
            });

          }
          else
          {
            var consumer = candidateConsumer.ElementAt(new Random().Next(0, candidateConsumer.Count() - 1));

            if (null != consumer)
            {
              Task.Run(() => consumer.Actor.Enqueue(@event));
            }
          }
        }
      }

      else
      {
        Parallel.ForEach(_actors.Where(actor => actor.Actor.StreamId == message.StreamId || actor.Actor.StreamId == StreamIds.AllStream), (actor) =>
        {
          Task.Run(() => actor.Actor.Enqueue(@event));

        });
      }


      return Task.CompletedTask;
    }
  }
}
