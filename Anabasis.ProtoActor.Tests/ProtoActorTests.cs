using Anabasis.Common;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;
using Polly;
using Proto;
using Proto.Mailbox;
using Proto.Router;
using System.Diagnostics;
using System.Reactive.Concurrency;
using Type = System.Type;

namespace Anabasis.ProtoActor.Tests
{

    public class ProtoActoTrade
    {
        public string CurrencyPair { get; set; }
        public double Rate { get; set; }
        public double Nominal { get; set; }
        public string Direction { get; set; }
        public string Counterparty { get; set; }
    }

    public class TradeActorOne : IActor
    {
        public TradeActorOne()
        {
        }

        public Task ReceiveAsync(IContext context)
        {
            Debug.WriteLine(context.Message?.GetType());
            return Task.CompletedTask;
        }
    }

    public interface IBufferTimeoutDelayMessage
    {

    }

    public interface IBufferingStrategy
    {
        void Reset();
        bool ShouldConsumeBuffer(object message, IContext context);
        bool ShouldConsumeBuffer(IBufferTimeoutDelayMessage timeoutMessage, IContext context);
    }

    public class GracefullyStopBufferActorMessage
    {

    }

    public class BufferTimeoutDelayMessage : IBufferTimeoutDelayMessage
    {

    }

    public class SlidingTimeoutBufferingStrategy : IBufferingStrategy
    {
        private readonly TimeSpan _absoluteTimeout;
        private readonly TimeSpan _slidingTimeout;
        private DateTime _lastMessageBufferizedUtcDate;

        public SlidingTimeoutBufferingStrategy(TimeSpan absoluteTimeout, TimeSpan slidingTimeout)
        {
            _absoluteTimeout = absoluteTimeout;
            _slidingTimeout = slidingTimeout;
            _lastMessageBufferizedUtcDate = DateTime.UtcNow;
        }

        public void Reset()
        {
            _lastMessageBufferizedUtcDate = DateTime.UtcNow;
        }

        public bool ShouldConsumeBuffer(object message, IContext context)
        {
            _lastMessageBufferizedUtcDate = DateTime.UtcNow;

            Scheduler.Default.Schedule(_slidingTimeout, () =>
            {
                context.Request(context.Self, new BufferTimeoutDelayMessage());
            });

            return false;

        }

        public bool ShouldConsumeBuffer(IBufferTimeoutDelayMessage timeoutMessage, IContext context)
        {

            if (_lastMessageBufferizedUtcDate.Add(_absoluteTimeout) >= DateTime.UtcNow)
            {
                if (_lastMessageBufferizedUtcDate.Add(_slidingTimeout) >= DateTime.UtcNow)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class AbsoluteTimeoutBufferingStrategy : IBufferingStrategy
    {
        private readonly TimeSpan _bufferConsumptionTimeout;
        private DateTime _lastConsumeBufferExecutionUtcDate;

        public AbsoluteTimeoutBufferingStrategy(TimeSpan bufferConsumptionTimeout)
        {
            _bufferConsumptionTimeout = bufferConsumptionTimeout;
            _lastConsumeBufferExecutionUtcDate = DateTime.UtcNow;
        }

        public void Reset()
        {
            _lastConsumeBufferExecutionUtcDate = DateTime.UtcNow;
        }

        public bool ShouldConsumeBuffer(object message, IContext context)
        {
            return _lastConsumeBufferExecutionUtcDate.Add(_bufferConsumptionTimeout) >= DateTime.UtcNow;
        }

        public bool ShouldConsumeBuffer(IBufferTimeoutDelayMessage timeoutMessage, IContext context)
        {
            return _lastConsumeBufferExecutionUtcDate.Add(_bufferConsumptionTimeout) >= DateTime.UtcNow;
        }
    }


    public class KillAppOnFailureSupervisorStrategy : ISupervisorStrategy
    {
        private readonly IKillSwitch _killSwitch;

        public KillAppOnFailureSupervisorStrategy(IKillSwitch killSwitch)
        {
            _killSwitch = killSwitch;
        }

        public void HandleFailure(ISupervisor supervisor, PID child, RestartStatistics rs, Exception cause, object? message)
        {
            _killSwitch.KillProcess(cause);
        }
    }

    public class BufferSizeBufferingStrategy : IBufferingStrategy
    {
        private long _currentBufferSize;
        private readonly long _bufferMaxSize;

        public BufferSizeBufferingStrategy(long bufferMaxSize)
        {
            _bufferMaxSize = bufferMaxSize;
        }

        public void Reset()
        {
            _currentBufferSize = 0;
        }

        public bool ShouldConsumeBuffer(object message, IContext context)
        {
            _currentBufferSize++;

            return _currentBufferSize >= _bufferMaxSize;
        }

        public bool ShouldConsumeBuffer(IBufferTimeoutDelayMessage timeoutMessage, IContext context)
        {
            return _currentBufferSize >= _bufferMaxSize;
        }
    }

    public abstract class MessageBufferActorBase : IActor
    {
        private readonly ILogger<MessageBufferActorBase> _logger;
        private readonly IBufferingStrategy[] _bufferingStrategies;
        private readonly List<object> _messageBuffer;
        private bool _shouldGracefulyStop;

        protected MessageBufferActorBase(IBufferingStrategy[] bufferingStrategies, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MessageBufferActorBase>();
            _bufferingStrategies = bufferingStrategies;
            _shouldGracefulyStop = false;
            _messageBuffer = new List<object>();
        }

        private bool ShouldConsumeBuffer(object message, IContext context)
        {
            if (_shouldGracefulyStop) return true;

            var shouldConsumeBuffer = false;

            foreach (var bufferingStrategy in _bufferingStrategies)
            {
                if (bufferingStrategy.ShouldConsumeBuffer(message, context))
                {
                    shouldConsumeBuffer = true;
                    break;
                }
            }

            return shouldConsumeBuffer;

        }

        private async Task ConsumeBuffer(IContext context)
        {
            foreach (var bufferingStrategy in _bufferingStrategies)
            {
                bufferingStrategy.Reset();
            }

            await ReceiveAsync(_messageBuffer.ToArray(), context);
        }

        public abstract Task ReceiveAsync(object[] messages, IContext context);

        public async Task ReceiveAsync(IContext context)
        {
            var message = context.Message;

            switch (message)
            {
                case SystemMessage:
                    _logger.LogDebug($"Received SystemMessage => {message.GetType()}");
                    break;
                case GracefullyStopBufferActorMessage:
                    _shouldGracefulyStop = true;
                    break;
                case IBufferTimeoutDelayMessage:
                default:

                    if (message is not IBufferTimeoutDelayMessage || _shouldGracefulyStop)
                    {
                        _messageBuffer.Add(message);
                    }

                    var shouldConsumeBuffer = ShouldConsumeBuffer(message, context);

                    if (shouldConsumeBuffer)
                    {
                        await ConsumeBuffer(context);

                        _messageBuffer.Clear();
                    }
                    else
                    {
                        if (message is not IBufferTimeoutDelayMessage)
                        {
                            _messageBuffer.Add(message);
                        }
                    }

                    if (_shouldGracefulyStop)
                    {
                        context.Stop(context.Self);
                    }

                    break;
            }

        }
    }

    public class TradeActorTwo : IActor
    {
        public TradeActorTwo()
        {
        }

        public Task ReceiveAsync(IContext context)
        {


            throw new NotImplementedException();
        }
    }

    public static class BusOneExtension
    {
        public static void SubscribeToBusOne(this ProtoActorSystem tradeSystem)
        {

            var busOne = tradeSystem.GetConnectedBus<BusOne<BusOneMessage>>();

            busOne.Subscribe((message) =>
            {
                tradeSystem.RootContext.Send(tradeSystem.PID, message);

                tradeSystem.ActorSystem.EventStream.Publish(message.Content);

                return Task.CompletedTask;

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

    public class BusOne<TMessage> : IBus
        where TMessage : IMessage
    {
        private readonly List<Func<TMessage, Task>> _subscribers = new();

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

        public void Subscribe(Func<TMessage, Task> onMessageReceived)
        {
            _subscribers.Add(onMessageReceived);
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }
        public void Emit(TMessage busOneMessage)
        {
            foreach(var subscriber in _subscribers)
            {
                subscriber(busOneMessage);
            }
        }
    }

    public class ProtoActorSystem
    {
        private readonly ActorSystem _actorSystem;
        private readonly RootContext _rootContext;
        private readonly PID _pid;
        private readonly Dictionary<Type, IBus> _connectedBus;

        public ActorSystem ActorSystem => _actorSystem;
        public RootContext RootContext => _rootContext;
        public PID PID => _pid;

        public ProtoActorSystem(Func<RootContext,PID> buildActorSytem)
        {
            _actorSystem = new ActorSystem();
            _rootContext = new RootContext(_actorSystem);
            _pid = buildActorSytem(_rootContext);
            _connectedBus = new Dictionary<Type, IBus>();
        }
        public Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false)
        {
            var busType = bus.GetType();

            if (_connectedBus.ContainsKey(busType))
            {
                throw new InvalidOperationException($"Bus of type {busType} is already registered");
            }

            _connectedBus[busType] = bus;

            return Task.CompletedTask;
        }

        public TBus GetConnectedBus<TBus>() where TBus : class, IBus
        {
            var busType = typeof(TBus);

            if (!_connectedBus.ContainsKey(busType))
            {

                var candidate = _connectedBus.Values.FirstOrDefault(bus => (bus as TBus) != null);

                if (null == candidate)
                {
                    throw new InvalidOperationException($"Bus of type {busType} is not registered");
                }

                return (TBus)candidate;
            }

            return (TBus)_connectedBus[busType];
        }
    }
    public class ProtoBufferActorPoolSystemBuilder<TActor>: ProtoActorPoolSystemBuilder<TActor> where TActor : MessageBufferActorBase
    {
        private readonly IBufferingStrategy[] _bufferingStrategies;

        public ProtoBufferActorPoolSystemBuilder(IBufferingStrategy[] bufferingStrategies,
            ISupervisorStrategy? supervisorStrategy = null,
            ISupervisorStrategy? chidSupervisorStrategy = null) : base(supervisorStrategy, chidSupervisorStrategy)
        {
            _bufferingStrategies = bufferingStrategies;
        }
    }

    enum ProtoActorPoolSystemType
    {
        RoundRobinPool,
        ConsistentHashPool
    }

    public class ProtoActorPoolSystemBuilder<TActor> where TActor : IActor
    {

        private readonly ISupervisorStrategy? _supervisorStrategy;
        private readonly ISupervisorStrategy? _chidSupervisorStrategy;
        private ProtoActorPoolSystemType _protoActorSystemType;
        private Func<RootContext, Props, PID>? _doBuildActor;
        private Func<Props, Props> _doBuildProps;

        public ProtoActorPoolSystemBuilder(ISupervisorStrategy? supervisorStrategy, ISupervisorStrategy? chidSupervisorStrategy = null)
        {
            _supervisorStrategy = supervisorStrategy;
            _chidSupervisorStrategy = chidSupervisorStrategy ?? supervisorStrategy;
            _doBuildProps = (props) => props;
        }

        public ProtoActorPoolSystemBuilder<TActor> WithPropsOverride(Func<Props,Props> onCreateProps)
        {
            _doBuildProps = onCreateProps;

            return this;
        }

        public ProtoActorPoolSystemBuilder<TActor> WithRoundRobinPool(int poolSize)
        {
            _protoActorSystemType = ProtoActorPoolSystemType.RoundRobinPool;

            _doBuildActor = new Func<RootContext, Props, PID>((rootContext, props) =>
            {
                var newRoundRobinPoolProps = rootContext.NewRoundRobinPool(props, 5);
                return rootContext.Spawn(props);
            });

            return this;
        }

        public ProtoActorPoolSystemBuilder<TActor> WithConsistentHashPool(int poolSize, int replicaCount = 100, Func<string, uint>? hash = null, Func<object, string>? messageHasher = null)
        {
            _protoActorSystemType = ProtoActorPoolSystemType.ConsistentHashPool;

            _doBuildActor = new Func<RootContext,Props, PID>((rootContext, props) =>
            {
                var newRoundRobinPoolProps = rootContext.NewConsistentHashPool(props, 5);
                return rootContext.Spawn(props);
            });

            return this;
        }

        public ProtoActorSystem<>


    }


    [TestFixture]
    public class ProtoActorTests
    {

        [Test]
        public async Task ShouldCreateAnActor()
        {

            var killSwitch = Substitute.For<IKillSwitch>();

            var tradeSystem = new ProtoActorSystem((context) =>
            {
                var props = new Props()
                    .WithProducer(() => new TradeActorOne())
                    .WithGuardianSupervisorStrategy(new KillAppOnFailureSupervisorStrategy(killSwitch));

                var newRoundRobinPoolProps = context.NewConsistentHashPool(props, 5);
                return context.Spawn(props);
            });

            var busOne = new BusOne<BusOneMessage>();

            await tradeSystem.ConnectTo(busOne);

            tradeSystem.SubscribeToBusOne();

            busOne.Emit(new BusOneMessage(new BusOneEvent()));


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