using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.Contracts;
using Anabasis.Common.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Worker.Tests.Model
{
    public static class TestEventBusExtensions
    {
        public static void SubscribeToTestEventBus(this IWorker worker)
        {
            var testEventBus = worker.GetConnectedBus<ITestEventBus>();

            testEventBus.Subscribe(async (messages) =>
            {
                await worker.Handle(messages);
            });
        }
    }

    public interface ITestEventBus : IBus
    {
        void Subscribe(Func<IMessage[], Task> onMessage);
    }

    public class TestEventBus : ITestEventBus
    {
        private readonly Random _rand;
        private readonly List<Func<IMessage[], Task>> _subscribers;
        private readonly Task _generateMessage;

        public List<IMessage> Messages { get; } = new List<IMessage>();

        private IMessage GetEvent()
        {
            IEvent @event = (_rand.Next(0, 2) == 0 ? new EventA(Guid.NewGuid(), "EventA") : new EventB(Guid.NewGuid(), "EventB"));
            return new TestEventBusMessage(@event);
        }

        public TestEventBus(int messageGenerationFrequencyInMilliseconds = 2000)
        {
            var cancellationTokenSource = new CancellationTokenSource();

            _rand = new Random();
            _subscribers = new List<Func<IMessage[], Task>>();

            _generateMessage = Task.Factory.StartNew(async (_) =>
            {

                while (!cancellationTokenSource.IsCancellationRequested)
                {
                    if (_subscribers.Count == 0) continue;

                    foreach (var subscriber in _subscribers)
                    {
                        var messageBatch = Enumerable.Range(0, 100).Select(_ => GetEvent()).Take(_rand.Next(1, 100)).ToArray();

                        Messages.AddRange(messageBatch);

                        await subscriber(messageBatch);
                    }

                    await Task.Delay(messageGenerationFrequencyInMilliseconds);
                }

            }, TaskContinuationOptions.LongRunning, cancellationTokenSource.Token);

        }

        public string BusId => $"{nameof(TestEventBus)}{Guid.NewGuid()}";

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

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

        public void Subscribe(Func<IMessage[], Task> onMessage)
        {
            _subscribers.Add(onMessage);
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

        public bool IsAcknowledge { get; private set; }

        public Guid? TraceId { get; set; }

        public bool IsAcknowledged => throw new NotImplementedException();

        public IObservable<bool> OnAcknowledged => throw new NotImplementedException();

        public Task Acknowledge()
        {
            IsAcknowledge = true;

            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string reason = null)
        {
            return Task.CompletedTask;
        }
    }

    public class EventA : BaseEvent
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

        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestWorker(IWorkerConfigurationFactory workerConfigurationFactory, ILoggerFactory loggerFactory = null) : base(workerConfigurationFactory, loggerFactory)
        {
        }

        public TestWorker(IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy = null, ILoggerFactory loggerFactory = null) : base(workerConfiguration, workerMessageDispatcherStrategy, loggerFactory)
        {
        }

        public async override Task Handle(IEvent[] messages)
        {
            Events.AddRange(messages);

            await Task.Delay(200);
        }
    }
}
