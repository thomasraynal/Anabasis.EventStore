using Anabasis.Common;
using Anabasis.Common.Worker;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.DependencyInjection;
using Proto.Mailbox;
using Proto.Router;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Anabasis.ProtoActor
{

    public class ProtoActorSystem : IProtoActorSystem
    {

        private readonly ISupervisorStrategy _supervisorStrategy;
        private readonly ISupervisorStrategy? _chidSupervisorStrategy;
        private readonly Dictionary<Type, IBus> _connectedBus;
        private readonly CompositeDisposable _cleanUp;
        private readonly List<PID> _rootPidRegistry;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly IProtoActorPoolDispatchQueue _protoActorPoolDispatchQueue;

        public ActorSystem ActorSystem { get; }
        public RootContext RootContext { get; }

        public long ProcessedMessagesCount { get; private set; }
        public long ReceivedMessagesCount { get; private set; }
        public long AcknowledgeMessagesCount { get; private set; }
        public long EnqueuedMessagesCount { get; private set; }

        public ILogger? Logger { get; }
        public string Id { get; }

        public ProtoActorSystem(ISupervisorStrategy supervisorStrategy,
            IProtoActorPoolDispatchQueueConfiguration protoActorPoolDispatchQueueConfiguration,
            IServiceProvider serviceProvider,
            ILoggerFactory? loggerFactory = null,
            IQueueBuffer? queueBuffer = null,
            ISupervisorStrategy? chidSupervisorStrategy = null,
            IKillSwitch? killSwitch = null)
        {
            _supervisorStrategy = supervisorStrategy;
            _chidSupervisorStrategy = chidSupervisorStrategy ?? supervisorStrategy;
            _connectedBus = new Dictionary<Type, IBus>();
            _cleanUp = new CompositeDisposable();
            _rootPidRegistry = new List<PID>();
            _cancellationTokenSource = new CancellationTokenSource();

            Logger = loggerFactory?.CreateLogger<ProtoActorSystem>();
            Id = $"{nameof(ProtoActorSystem)}_{Guid.NewGuid()}";

            _protoActorPoolDispatchQueue = new ProtoActorPoolDispatchQueue(Id, protoActorPoolDispatchQueueConfiguration, _cancellationTokenSource.Token,
                ProcessMessage,
                queueBuffer,
                loggerFactory,
                killSwitch);

            _cleanUp.Add(_protoActorPoolDispatchQueue);

            ActorSystem = new ActorSystem().WithServiceProvider(serviceProvider);
            RootContext = new RootContext(ActorSystem);
        }

        public PID CreateRoundRobinPool<TActor>(int poolSize, Action<Props>? onCreateProps = null) where TActor : IActor
        {
            var props = CreateCommonProps<TActor>(onCreateProps).WithMailbox(() => UnboundedMailbox.Create());

            var newRoundRobinPoolProps = RootContext.NewRoundRobinPool(props, poolSize);

            var pid = RootContext.Spawn(newRoundRobinPoolProps);

            _rootPidRegistry.Add(pid);
            
            return pid;

        }

        private Props CreateCommonProps<TActor>(Action<Props>? onCreateProps = null) where TActor : IActor
        {
            var props = ActorSystem.DI().PropsFor<TActor>();

            props.WithGuardianSupervisorStrategy(_supervisorStrategy);

            if (null != _chidSupervisorStrategy)
            {
                props.WithChildSupervisorStrategy(_chidSupervisorStrategy);
            }

            onCreateProps?.Invoke(props);

            return props;
        }

        public PID CreateConsistentHashPool<TActor>(int poolSize, int replicaCount = 100, Action<Props>? onCreateProps = null, Func<string, uint>? hash = null, Func<object, string>? messageHasher = null) where TActor : IActor
        {
            var props = CreateCommonProps<TActor>(onCreateProps).WithMailbox(() => UnboundedMailbox.Create());

            var consistentHashPoolProps = RootContext.NewConsistentHashPool(props, poolSize, hash, replicaCount, messageHasher);

            var pid = RootContext.Spawn(consistentHashPoolProps);

            _rootPidRegistry.Add(pid);

            return pid;
        }

        public PID CreateActor<TActor>(Action<Props>? onCreateProps = null) where TActor : IActor
        {
            var props = CreateCommonProps<TActor>(onCreateProps).WithMailbox(() => UnboundedMailbox.Create());

            var pid = RootContext.Spawn(props);

            _rootPidRegistry.Add(pid);

            return pid;
        }

        private void ProcessMessage(IMessage[] messages)
        {
            foreach (var pid in _rootPidRegistry)
            {
                foreach (var message in messages)
                {
                    RootContext.Send(pid, message);
                }
            }

            ProcessedMessagesCount += messages.Length;
        }

        public Task Send(IMessage message, TimeSpan? timeout = null)
        {
            return Send(new[] { message });
        }

        public Task Send(IMessage[] messages, TimeSpan? timeout = null)
        {

            timeout = timeout == null ? TimeSpan.FromMinutes(30) : timeout.Value;

            var now = DateTime.UtcNow;
            var messagesToProcess = messages;

            ReceivedMessagesCount += messages.Length;

            var taskCompletionSource = new TaskCompletionSource();

            var onMessageBatchAck = Observable.Merge(messages.Select(message => message.OnAcknowledged)).Subscribe(_ =>
            {
                AcknowledgeMessagesCount++;

                Logger?.LogDebug($"Message acknowledged");

                var isMessageBatchAcknowledged = messages.All(message => message.IsAcknowledged);

                if (isMessageBatchAcknowledged)
                {
                    taskCompletionSource.SetResult();

                    Logger?.LogDebug($"Finalized batch of {messages.Length} acknowledged");
                }

            });

            Scheduler.CurrentThread.Schedule(() =>
            {

                while (messagesToProcess.Length > 0)
                {

                    if (now.Add(timeout.Value) <= DateTime.UtcNow)
                    {
                        throw new TimeoutException("Unable to process message - timeout reached");
                    }

                    Logger?.LogDebug($"Handle batch of {messagesToProcess.Length}");

                    var processedMessages = _protoActorPoolDispatchQueue.TryEnqueue(messagesToProcess, out var unProcessedMessages);

                    EnqueuedMessagesCount += processedMessages.Length;
                    messagesToProcess = unProcessedMessages;

                }

            });

            return taskCompletionSource.Task.ContinueWith(_ => onMessageBatchAck.Dispose());

        }

        public Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false)
        {
            var busType = bus.GetType();

            if (_connectedBus.ContainsKey(busType))
            {
                throw new InvalidOperationException($"Bus of type {busType} is already registered");
            }

            _connectedBus[busType] = bus;

            if (closeUnderlyingSubscriptionOnDispose)
            {
                _cleanUp.Add(bus);
            }

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

        public void Dispose()
        {
            _cleanUp.Dispose();

            ActorSystem.ShutdownAsync().Wait();

        }

    }
}
