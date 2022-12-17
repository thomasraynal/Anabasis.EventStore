using Anabasis.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Proto;
using Proto.DependencyInjection;
using Proto.Mailbox;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Subjects;

namespace Anabasis.ProtoActor.Tests
{

    public class DummyLogger : ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return Disposable.Empty;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            Debug.WriteLine(state);
        }
    }

    public class DummyLoggerFactory : ILoggerFactory
    {
        public void AddProvider(ILoggerProvider provider)
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DummyLogger();
        }

        public void Dispose()
        {
        }
    }

    public class TestActor : IActor
    {
        public async Task ReceiveAsync(IContext context)
        {
            if(context.Message is SystemMessage)
            {
                return;
            }

            var rand = new Random();

            await Task.Delay(rand.Next(100, 500));

            var ev = context.Message as BusOneMessage;

            Debug.WriteLine($"Handle message {(ev.Content as BusOneEvent).EventNumber}");



        }
    }

    public class TestMessageBufferActor : MessageBufferActorBase
    {
        public TestMessageBufferActor(MessageBufferActorConfiguration messageBufferActorConfiguration, ILoggerFactory? loggerFactory = null) : base(messageBufferActorConfiguration, loggerFactory)
        {
        }

        public override async Task ReceiveAsync(object[] messages, IContext context)
        {

            var rand = new Random();

            await Task.Delay(rand.Next(100, 500));

            Debug.WriteLine($"{context.Self.Id} handle batch of {messages.Length} messages");

            foreach (var message in messages.Cast<IMessage>())
            {
                await message.Acknowledge();
            }

        }
    }

    public static class BusOneExtension
    {
        public static void SubscribeToBusOne(this IProtoActorSystem protoActorSystem)
        {

            var busOne = protoActorSystem.GetConnectedBus<BusOne>();

            busOne.Subscribe(async (messages) =>
            {
                await protoActorSystem.Send(messages);

            });
        }
    }

    public class BusOneMessage : IMessage
    {
        private readonly Subject<bool> _onAcknowledgeSubject;

        public BusOneMessage(IEvent @event)
        {
            Content = @event;
            _onAcknowledgeSubject = new Subject<bool>();
        }

        public Guid? TraceId => Guid.NewGuid();

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content { get; }

        public bool IsAcknowledged { get; private set; }

        public IObservable<bool> OnAcknowledged => _onAcknowledgeSubject;

        public Task Acknowledge()
        {
            IsAcknowledged = true;

            _onAcknowledgeSubject.OnNext(true);
            _onAcknowledgeSubject.OnCompleted();

            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string? reason = null)
        {

            _onAcknowledgeSubject.OnNext(false);
            _onAcknowledgeSubject.OnCompleted();
            return Task.CompletedTask;
        }
    }

    public class BusOne : IBus
    {
        private readonly List<Func<IMessage[], Task>> _subscribers = new();

        public string BusId => Guid.NewGuid().ToString();

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void Subscribe(Func<IMessage[], Task> onMessageReceived)
        {
            _subscribers.Add(onMessageReceived);
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        public List<IMessage> EmittedMessages { get; private set; } = new List<IMessage>();

        public void Emit(IMessage busOneMessage)
        {

            foreach (var subscriber in _subscribers)
            {
                subscriber(new[] { busOneMessage });
            }
        }

        public void Emit(IMessage[] busOneMessages)
        {

            EmittedMessages.AddRange(busOneMessages);

            foreach (var subscriber in _subscribers)
            {
                subscriber(busOneMessages);
            }
        }
    }



    // handle https://proto.actor/docs/receive-timeout/

    [TestFixture]
    public class ProtoActorTests
    {

        [Test]
        public async Task ShouldCreateAnActor()
        {
            var container = new Lamar.Container(serviceRegistry =>
            {
                serviceRegistry.For<TestMessageBufferActor>().Use<TestMessageBufferActor>();
                serviceRegistry.For<ILoggerFactory>().Use<DummyLoggerFactory>();

                serviceRegistry.For<MessageBufferActorConfiguration>().Use((_) =>
                {
                    var messageBufferActorConfiguration = new MessageBufferActorConfiguration(TimeSpan.FromSeconds(1), new IBufferingStrategy[]
                    {
                        new AbsoluteTimeoutBufferingStrategy(TimeSpan.FromSeconds(1)),
                        new BufferSizeBufferingStrategy(5)
                    });

                    return messageBufferActorConfiguration;
                });
            });

            var actorSystem = new ActorSystem().WithServiceProvider(container);

            var killSwitch = Substitute.For<IKillSwitch>();
            // var props = actorSystem.DI().PropsFor<TestActor>().WithMailbox(() => UnboundedMailbox.Create());
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);
            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(int.MaxValue, true);

            var protoActoSystem = new ProtoActorSystem(supervisorStrategy,
              protoActorPoolDispatchQueueConfiguration,
              container.ServiceProvider,
              new DummyLoggerFactory());

            var pid = protoActoSystem.CreateActor<TestActor>();
            var busOne = new BusOne();

            await protoActoSystem.ConnectTo(busOne);

            protoActoSystem.SubscribeToBusOne();

            var rand = new Random();

            for (var i = 0; i < 100; i++)
            {
                busOne.Emit(new BusOneMessage(new BusOneEvent(i)));
            }

            await Task.Delay(5000);

        }


        [Test]
        public async Task ShouldCreateAnActorPool()
        {
           
            var container = new Lamar.Container(serviceRegistry =>
            {
                serviceRegistry.For<TestMessageBufferActor>().Use<TestMessageBufferActor>();
                serviceRegistry.For<ILoggerFactory>().Use<DummyLoggerFactory>();
                
                serviceRegistry.For<MessageBufferActorConfiguration>().Use((_) =>
                {
                    var messageBufferActorConfiguration = new MessageBufferActorConfiguration(TimeSpan.FromSeconds(1), new IBufferingStrategy[]
                    {
                        new AbsoluteTimeoutBufferingStrategy(TimeSpan.FromSeconds(1)),
                        new BufferSizeBufferingStrategy(5)
                    });

                    return messageBufferActorConfiguration;
                });
            });


            var actorSystem = new ActorSystem().WithServiceProvider(container);

            var props = actorSystem.DI().PropsFor<TestMessageBufferActor>().WithMailbox(() => UnboundedMailbox.Create()).WithMailbox(() => UnboundedMailbox.Create());


            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);

            var protoActorPoolDispatchQueueConfiguration = new ProtoActorPoolDispatchQueueConfiguration(int.MaxValue, true);

            var protoActoSystem = new ProtoActorSystem(supervisorStrategy, 
                    protoActorPoolDispatchQueueConfiguration, 
                    container.ServiceProvider,
                    new DummyLoggerFactory());

            var ruondRobinPool = protoActoSystem.CreateRoundRobinPool<TestMessageBufferActor>(5);

            var busOne = new BusOne();

            await protoActoSystem.ConnectTo(busOne);

            protoActoSystem.SubscribeToBusOne();

            var rand = new Random();

            for (var i = 0; i < 100; i++)
            {
                busOne.Emit(Enumerable.Range(0,rand.Next(1,10)).Select(_=> new BusOneMessage(new BusOneEvent(i))).ToArray());
            }

            await Task.Delay(10000);

            var unackedMessages = busOne.EmittedMessages.Where(message => !message.IsAcknowledged).ToArray();

        }

    }
}