using Anabasis.Common;
using Anabasis.Common.Shared;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.Poc
{

    public class EventStoreBus : IBus
    {
        public bool IsConnected => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void DoHealthCheck(bool shouldThrow = false)
        {
            throw new NotImplementedException();
        }

        public void Emit<TEvent>()
        {
            throw new NotImplementedException();
        }

        public Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders)
        {
            throw new NotImplementedException();
        }

        public Task Emit(IEnumerable<IEvent> events, params KeyValuePair<string, string>[] extraHeaders)
        {
            throw new NotImplementedException();
        }

        public Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEntity
        {
            throw new NotImplementedException();
        }

        public Task<IAnabasisHealthCheck> GetHealthCheck(bool shouldThrowIfUnhealthy = false)
        {
            throw new NotImplementedException();
        }

        public Task<TCommandResult> Send<TCommand, TCommandResult>(TCommand command)
        {
            throw new NotImplementedException();
        }

        public IObservable<TEvent> Subscribe<TEvent>()
        {
            throw new NotImplementedException();
        }
    }

    public class OneEventStoreEvent : IEventStoreEvent
    {
        public OneEventStoreEvent()
        {
            EventID = Guid.NewGuid();
            CorrelationID = Guid.NewGuid();
        }

        public Guid EventID { get; }

        public Guid CorrelationID { get; }

        public bool IsCommand => false;

        public BusType BusType => throw new NotImplementedException();

        public string EntityId => throw new NotImplementedException();
    }

    public class OneRabbitMQEvent : IRabbitMQEvent
    {
        public OneRabbitMQEvent()
        {
            EventID = Guid.NewGuid();
            CorrelationID = Guid.NewGuid();
        }

        public Guid EventID { get; }

        public Guid CorrelationID { get; }

        public bool IsCommand => false;

        public BusType BusType => throw new NotImplementedException();

        public string EntityId => Subject;

        public string Subject => throw new NotImplementedException();
    }

    public class RabbitMQBus : IBus
    {
        public BusType BusType => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void DoHealthCheck(bool shouldThrow = false)
        {
            throw new NotImplementedException();
        }

        public void Emit<TEvent>()
        {
            throw new NotImplementedException();
        }

        public Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders)
        {
            throw new NotImplementedException();
        }

        public Task Emit(IEnumerable<IEvent> events, params KeyValuePair<string, string>[] extraHeaders)
        {
            throw new NotImplementedException();
        }

        public Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEntity
        {
            throw new NotImplementedException();
        }

        public Task<IAnabasisHealthCheck> GetHealthCheck(bool shouldThrowIfUnhealthy = false)
        {
            throw new NotImplementedException();
        }

        public Task<TCommandResult> Send<TCommand, TCommandResult>(TCommand command)
        {
            throw new NotImplementedException();
        }

        public IObservable<TEvent> Subscribe<TEvent>()
        {
            throw new NotImplementedException();
        }
    }

    public interface IRabbitMQEvent : IEvent
    {
        string Subject { get; }
    }

    public interface IEventStoreEvent : IEvent
    {
    }

    public class Actor : IActor
    {
        public string Id => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public Task EmitEventStore<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent
        {
            throw new NotImplementedException();
        }

        public Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {
            throw new NotImplementedException();
        }

        public void ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false)
        {
            throw new NotImplementedException();
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public TBus GetConnectedBus<TBus>() where TBus: class
        {
            throw new NotImplementedException();
        }

        public void SubscribeToEventStream(IEventStoreStream eventStoreStream, bool closeUnderlyingSubscriptionOnDispose = false)
        {
            throw new NotImplementedException();
        }

        public Task OnEventReceived(IEvent @event)
        {
            throw new NotImplementedException();
        }

        public void AddDisposable(IDisposable disposable)
        {
            throw new NotImplementedException();
        }

        public void ConnectTo<TInterface>(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task OnEventReceived(IEvent @event, TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public Task<IAnabasisHealthCheck[]> GetActorHealthChecks()
        {
            throw new NotImplementedException();
        }

    }

    public interface IStatefulActor : IStatelessActor
    {
        string State { get; }
    }

    public interface IStatelessActor : IActor
    {
    }

    [TestFixture]
    public class TestPOC
    {
        [Test]
        public async Task ShouldTestBusRegistration()
        {
            //var actor = new Actor();

            //var rabbitMQBus = new RabbitMQBus();
            //var eventStoreBus = new EventStoreBus();

            //actor.ConnectTo(eventStoreBus);
            //actor.ConnectTo(rabbitMQBus);

            //await actor.EmitEventStore(new OneEventStoreEvent());
        }
    }

}
