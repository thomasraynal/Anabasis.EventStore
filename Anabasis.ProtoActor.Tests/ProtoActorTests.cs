using Anabasis.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Proto;
using Proto.Mailbox;
using Proto.Router;
using System.Diagnostics;

namespace Anabasis.ProtoActor.Tests
{
    public class TradeActor : IActor
    {
        public Task ReceiveAsync(IContext context)
        {
            Debug.WriteLine(context.Message.GetType());

            return Task.CompletedTask;
        }
    }

    public class TradeActorOne : MessageBufferActorBase
    {
        public TradeActorOne(IBufferingStrategy[] bufferingStrategies, ILoggerFactory loggerFactory) : base(bufferingStrategies, loggerFactory)
        {
        }

        public override Task ReceiveAsync(object[] messages, IContext context)
        {
            Debug.WriteLine($"Received message group => {messages.Length}");

            return Task.CompletedTask;
        }
    }

    public static class BusOneExtension
    {
        public static void SubscribeToBusOne(this IProtoActorSystem protoActorSystem)
        {

            var busOne = protoActorSystem.GetConnectedBus<BusOne>();

            busOne.Subscribe(async (message) =>
            {
                await protoActorSystem.OnMessageReceived(message);

            });
        }
    }

    public class BusOneMessage : IMessage
    {
        public BusOneMessage(IEvent @event)
        {
            Content = @event;
        }

        public Guid? TraceId => Guid.NewGuid();

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content { get; }

        public Task Acknowledge()
        {
            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string? reason = null)
        {
            return Task.CompletedTask;
        }
    }

    public class BusOne : IBus
    {
        private readonly List<Func<IMessage, Task>> _subscribers = new();

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

        public void Subscribe(Func<IMessage, Task> onMessageReceived)
        {
            _subscribers.Add(onMessageReceived);
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }
        public void Emit(IMessage busOneMessage)
        {
            foreach(var subscriber in _subscribers)
            {
                subscriber(busOneMessage);
            }
        }
    }


    public class TestServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
        {
            return
        }
    }

    //use lamar
    // handle https://proto.actor/docs/receive-timeout/

    [TestFixture]
    public class ProtoActorTests
    {

        [Test]
        public async Task ShouldCreateAnActor()
        {

            var killSwitch = Substitute.For<IKillSwitch>();
            var supervisorStrategy = new KillAppOnFailureSupervisorStrategy(killSwitch);

            var bufferingStrategies = new IBufferingStrategy[]
            {
                new BufferSizeBufferingStrategy(5),
                new AbsoluteTimeoutBufferingStrategy(TimeSpan.FromSeconds(5))
            };

        


            var testServiceProvider = new TestServiceProvider();

            var protoActorPoolSystem = new ProtoActorPoolSystem(supervisorStrategy, testServiceProvider);

            var ruondRobinPool = protoActorPoolSystem.CreateRoundRobinPool<TradeActor>(2);

            var busOne = new BusOne();

            await protoActorPoolSystem.ConnectTo(busOne);

            protoActorPoolSystem.SubscribeToBusOne();

            busOne.Emit(new BusOneMessage(new BusOneEvent()));

            await Task.Delay(1000);


            //subscribe to the eventstream via type
            //system.EventStream.Subscribe<object>(x => Console.WriteLine($"Got message for {x.Name}"));
            //system.EventStream.SubscribeToTopic<SomeMessage>("MyTopic.*", x => Console.WriteLine($"Got message for {x.Name}"));


            //var context = new RootContext(system);
            //var props = context.NewConsistentHashPool(MyActorProps, 5);
            //var pid = context.Spawn(props);

            //var context = new RootContext(system);
            //var props = context.NewRoundRobinPool(MyActorProps, 5);
            //var pid = context.Spawn(props);

            //Console.WriteLine("Actor system created");

            //var eventProvider = new EventStoreProvider();
            //var persistence = Persistence.WithEventSourcingAndSnapshotting(
            //   eventProvider,
            //   eventProvider,
            //   "demo-app-id",
            //   (ev)=> { },
            //   (snapshot) => { });



            //var props = Props.FromProducer(() => new AnabasisActor())
            //    .WithDispatcher(new ThreadPoolDispatcher { Throughput = 300 })
            //    .WithMailbox(() => UnboundedMailbox.Create());

            //var behavior = new Behavior();

            ////behavior.Become();

            //var pid = system.Root.Spawn(props);


            //var rootContext = new RootContext(system);

            //var mailBox = UnboundedMailbox.Create();

            //var props = new Props()
            //    .WithProducer(() => new TradeActor())
            //    .WithDispatcher(new ThreadPoolDispatcher { Throughput = 300 })
            //    .WithMailbox(() => UnboundedMailbox.Create())
            //    .WithChildSupervisorStrategy(new OneForOneStrategy((who, reason) => SupervisorDirective.Restart, 10, TimeSpan.FromSeconds(10)))
            //    .WithReceiverMiddleware(
            //        next => async (c, envelope) =>
            //        {
            //            Console.WriteLine($"middleware 1 enter {envelope.Message.GetType()}:{envelope.Message}");
            //            await next(c, envelope);
            //            Console.WriteLine($"middleware 1 exit");
            //        })
            //    .WithSenderMiddleware(
            //        next => async (c, target, envelope) =>
            //        {
            //            Console.WriteLine($"middleware 1 enter {c.Message.GetType()}:{c.Message}");
            //            await next(c, target, envelope);
            //            Console.WriteLine($"middleware 1 enter {c.Message.GetType()}:{c.Message}");
            //        },
            //        next => async (c, target, envelope) =>
            //        {
            //            Console.WriteLine($"middleware 2 enter {c.Message.GetType()}:{c.Message}");
            //            await next(c, target, envelope);
            //            Console.WriteLine($"middleware 2 enter {c.Message.GetType()}:{c.Message}");
            //        })
            //    // the default spawner constructs the Actor, Context and Process
            //    .WithSpawner(Props.DefaultSpawner);


        }

    }
}