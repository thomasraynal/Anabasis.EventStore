using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.Contracts;
using Anabasis.Common.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Worker.Tests
{

    public static class TestEventBusExtensions
    {
        public static void Subscribe(this IWorker worker)
        {
            var testEventBus = worker.GetConnectedBus<ITestEventBus>();

           // testEventBus.Subscribe(worker.OnMessage);
        }
    }

    public interface ITestEventBus: IBus
    {
        void Subscribe(Func<IMessage, Task> onMessage);
    }

    public class TestEventBus: ITestEventBus
    {
        private readonly List<Func<IMessage, Task>> _subscribers;
        private readonly Task _generateMessage;

        public TestEventBus()
        {
            var cancellationTokenSource = new CancellationTokenSource();

            _subscribers = new List<Func<IMessage, Task>>();
            _generateMessage = Task.Factory.StartNew((_) =>
            {

                var rand = new Random();

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    IEvent @event = (rand.Next(0, 2) == 0 ? new EventA(Guid.NewGuid(),"EventA") : new EventB(Guid.NewGuid(), "EventB"));

                    foreach (var subscriber in _subscribers)
                    {
                        subscriber(new TestEventBusMessage(@event));
                    }
                }

            }, TaskContinuationOptions.LongRunning, cancellationTokenSource.Token);

        }

        public string BusId => $"{nameof(TestEventBus)}{Guid.NewGuid()}";

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public void Subscribe(Func<IMessage, Task> onMessage)
        {
            _subscribers.Add(onMessage);
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy("healthcheck from TestEventBus", new Dictionary<string, object>()
            {
                {"TestEventBus", "ok"}
            }));
        }

        public void Dispose()
        {
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            return Task.CompletedTask;
        }
    }

    public class TestEventBusMessage : IMessage
    {
        public TestEventBusMessage(IEvent content)
        {
            Content = content;
            MessageId = Guid.NewGuid();
        }

        public Guid MessageId { get; }

        public IEvent Content { get; }

        public Task Acknowledge()
        {
            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string reason = null)
        {
            return Task.CompletedTask;
        }
    }

    public class EventA: BaseEvent
    {
        public static EventA New()
        {
            return new EventA(Guid.NewGuid(), "A");
        }

        public EventA(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }
    }

    public class EventB : BaseEvent
    {
        public EventB(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }
    }

    public class EventC : BaseEvent
    {
        public EventC(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }
    }

    public class TestWorker : BaseWorker
    {

        public TestWorker(IWorkerConfigurationFactory workerConfigurationFactory, ILoggerFactory loggerFactory = null) : base(workerConfigurationFactory, loggerFactory)
        {
        }

        public TestWorker(IWorkerConfiguration workerConfiguration, ILoggerFactory loggerFactory = null) : base(workerConfiguration, loggerFactory)
        {
        }

        public override Task Handle(IEvent[] messages)
        {
            throw new NotImplementedException();
        }
    }
    

    [TestFixture]
    public class WorkerTests
    {
        [OneTimeSetUp]
        public void Setup()
        {

        }

        [Test]
        public Task ShouldCreateAWorker()
        {
            var testWorker = new TestWorker(new WorkerConfiguration());

         

            return Task.CompletedTask;
        }
    }
}
