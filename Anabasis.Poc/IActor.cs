using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Anabasis.Poc
{
    public interface IBus
    {
        BusType BusType { get; }
        Task<TCommandResult> Send<TCommand, TCommandResult>(TCommand command);
        void Emit<TEvent>();
        IObservable<TEvent> Subscribe<TEvent>();
    }

    public class EventStoreBus : IBus
    {
        public BusType BusType => BusType.EventStore;

        public void Emit<TEvent>()
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

        public string StreamId => "StreamId";

        public BusType BusType => throw new NotImplementedException();
    }

    public class OneRabbitMQEvent : IEvent
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
    }

    public class RabbitMQBus : IBus
    {
        public void Emit<TEvent>()
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

    public interface IHaveAStreamId
    {
        string StreamId { get; }
    }

    public interface IEventStoreEvent : IEvent, IHaveAStreamId
    {

    }

    public interface IEvent
    {
        Guid EventID { get; }
        Guid CorrelationID { get; }
        bool IsCommand { get; }
        BusType BusType { get; }
    }

    public enum BusType
    {
        EventStore,
        RabbitMQ,
        InMemory
    }

    public interface IActor
    {
        string Id { get; }
        bool IsConnected { get; }
        Task WaitUntilConnected(TimeSpan? timeout = null);
        Task<TCommandResult> Send<TCommand, TCommandResult>(TCommand command);
        void Emit<TEvent>(TEvent @event) where TEvent : IEvent;
        void SubscribeTo(IBus bus, bool closeSubscriptionOnDispose = false);
    }

    public class Actor : IActor
    {
        public Actor()
        {
        }

        public string Id => throw new NotImplementedException();

        public bool IsConnected => throw new NotImplementedException();

        public void Emit<TEvent>(TEvent @event) where TEvent : IEvent
        {
            throw new NotImplementedException();
        }

        public Task<TCommandResult> Send<TCommand, TCommandResult>(TCommand command)
        {
            throw new NotImplementedException();
        }

        public void SubscribeTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false)
        {
            throw new NotImplementedException();
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
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
            var actor = new Actor();

            var rabbitMQBus = new RabbitMQBus();
            var eventStoreBus = new EventStoreBus();

            actor.SubscribeTo(eventStoreBus);
            actor.SubscribeTo(rabbitMQBus);

            actor.Emit(new OneEventStoreEvent());
        }
    }

}
