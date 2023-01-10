using Anabasis.Common;
using Anabasis.Common.Worker;
using Anabasis.ProtoActor.Queue;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.DependencyInjection;
using Proto.Mailbox;
using Proto.Router;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.Common.Utilities;
using System.Diagnostics;
using Proto.Context;
using DynamicData;

namespace Anabasis.ProtoActor.System
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
        private readonly EventStreamSubscription<object> _deadLettersSubscription;

        //todo: switch to dictionnary
        private readonly List<(PID pid, ActorContext actorContext)> _spawnedActorContext;

        public ActorSystem ActorSystem { get; }
        public RootContext RootContext { get; }

        public long ProcessedMessagesCount { get; private set; }
        public long ReceivedMessagesCount { get; private set; }
        public long AcknowledgeMessagesCount { get; private set; }
        public long EnqueuedMessagesCount { get; private set; }

        public ILogger? Logger { get; }
        public string Id { get; }

        //https://github.com/asynkron/protoactor-dotnet/blob/dadcffbdacead2258d2606a1810ca3b5b1850069/src/Proto.Actor/Props/Props.cs#L83
        private PID SpawnerWithActorReference(ActorSystem system, string name, Props props, PID? parent, Action<IContext>? callback)
        {
            //Ordering is important here
            //first we create a mailbox and attach it to a process
            props = system.ConfigureProps(props);
            var mailbox = props.MailboxProducer();
            var dispatcher = props.Dispatcher;
            var process = new ActorProcess(system, mailbox);

            //then we register it to the process registry
            var (self, absent) = system.ProcessRegistry.TryAdd(name, process);
            //if this fails we exit and the process and mailbox is Garbage Collected
            if (!absent) throw new ProcessNameExistException(name, self);

            //if successful, we create the actor and attach it to the mailbox
            var ctx = ActorContext.Setup(system, props, parent, self, mailbox);
            callback?.Invoke(ctx);
            mailbox.RegisterHandlers(ctx, dispatcher);

            _spawnedActorContext.Add((self, ctx));

            mailbox.PostSystemMessage(Started.Instance);

            //finally, start the mailbox and make the actor consume messages
            mailbox.Start();

            return self;
        }

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
            _spawnedActorContext = new List<(PID pid, ActorContext actor)>();
            _cancellationTokenSource = new CancellationTokenSource();

            Logger = loggerFactory?.CreateLogger<ProtoActorSystem>();
            Id = this.GetUniqueIdFromType();

            _protoActorPoolDispatchQueue = new ProtoActorPoolDispatchQueue(Id, protoActorPoolDispatchQueueConfiguration, _cancellationTokenSource.Token,
                ProcessMessage,
                queueBuffer,
                loggerFactory,
                killSwitch);

            _cleanUp.Add(Disposable.Create(() => _cancellationTokenSource.Cancel()));
            _cleanUp.Add(_protoActorPoolDispatchQueue);

            var actorSystemConfig = new ActorSystemConfig()
            {
                ConfigureSystemProps = (_, props) =>
                {
                    props.WithChildSupervisorStrategy(_supervisorStrategy)
                         .WithGuardianSupervisorStrategy(_supervisorStrategy);
                    return props;
                }
            };

            ActorSystem = new ActorSystem(actorSystemConfig).WithServiceProvider(serviceProvider);

            RootContext = new RootContext(ActorSystem);

            _deadLettersSubscription = ActorSystem.EventStream.Subscribe<DeadLetterEvent>(
                 deadLetterEvent =>
                 {
                     var logMessage = $"Received dead letter : {Environment.NewLine} {deadLetterEvent.ToJson()}";

                     Logger?.LogError(logMessage);
                 });

            _cleanUp.Add(Disposable.Create(() => _deadLettersSubscription.Unsubscribe()));

        }

        private PID SpawnAndKeepTrackOfActorContext(Props props)
        {
            var id = ActorSystem.ProcessRegistry.NextId();

            var pid = RootContext.SpawnNamed(props, id, (context) =>
            {
                if (context is not ActorContext actorContext)
                {
                    throw new InvalidOperationException($"{context.GetType()} is not of type {typeof(ActorContext)}");
                }

                _spawnedActorContext.Add((actorContext.Self, actorContext));

            });

            return pid;
        }

        public PID CreateRoundRobinPool<TActor>(int poolSize, Action<Props>? onCreateProps = null) where TActor : IActor
        {
            var props = CreateCommonProps<TActor>(onCreateProps);

            var newRoundRobinPoolProps = RootContext.NewRoundRobinPool(props, poolSize);

            var pid = SpawnAndKeepTrackOfActorContext(newRoundRobinPoolProps);

            _rootPidRegistry.Add(pid);

            return pid;

        }

        private Props CreateCommonProps<TActor>(Action<Props>? onCreateProps = null) where TActor : IActor
        {

            var props = ActorSystem.DI().PropsFor<TActor>()
                                        .WithExceptionHandler(Logger)
                                        .WithGuardianSupervisorStrategy(_supervisorStrategy)
                                        .WithSpawner(SpawnerWithActorReference);

            if (null != _chidSupervisorStrategy)
            {
                props.WithChildSupervisorStrategy(_chidSupervisorStrategy);
            }

            onCreateProps?.Invoke(props);

            return props;
        }

        public PID CreateConsistentHashPool<TActor>(int poolSize, int replicaCount = 100, Action<Props>? onCreateProps = null, Func<string, uint>? hash = null, Func<object, string>? messageHasher = null) where TActor : IActor
        {
            var props = CreateCommonProps<TActor>(onCreateProps);

            var consistentHashPoolProps = RootContext.NewConsistentHashPool(props, poolSize, hash, replicaCount, messageHasher);

            var pid = SpawnAndKeepTrackOfActorContext(consistentHashPoolProps);

            _rootPidRegistry.Add(pid);

            return pid;
        }

        public PID[] CreateActors<TActor>(int instanceCount, Action<Props>? onCreateProps = null) where TActor : IActor
        {
            var pids = new List<PID>();

            foreach (var _ in Enumerable.Range(0, instanceCount))
            {

                var props = CreateCommonProps<TActor>(onCreateProps);

                var pid = SpawnAndKeepTrackOfActorContext(props);

                _rootPidRegistry.Add(pid);

                pids.Add(pid);

            }

            return pids.ToArray();

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

        public void SendInternal(IMessage[] messages, TimeSpan timeout)
        {
            Scheduler.Default.Schedule(async () =>
            {
                var now = DateTime.UtcNow;
                var messagesToProcess = messages;

                while (messagesToProcess.Length > 0)
                {

                    if (now.Add(timeout) <= DateTime.UtcNow)
                    {
                        throw new TimeoutException("Unable to process message - timeout reached");
                    }

                    Logger?.LogDebug($"Trying to enqueue batch of {messagesToProcess.Length}");

                    var processedMessages = _protoActorPoolDispatchQueue.TryEnqueue(messagesToProcess, out var unProcessedMessages);

                    Logger?.LogDebug($"Processed {processedMessages.Count()}/{messagesToProcess.Length}");

                    EnqueuedMessagesCount += processedMessages.Length;
                    messagesToProcess = unProcessedMessages;

                    await Task.Delay(200);

                }
            });
        }

        public Task Send(IMessage[] messages, TimeSpan? timeout = null)
        {

            timeout = timeout == null ? TimeSpan.FromMinutes(30) : timeout.Value;

            ReceivedMessagesCount += messages.Length;

            var taskCompletionSource = new TaskCompletionSource();

            var onMessageBatchAck = messages.Select(message => message.OnAcknowledged).Merge().Subscribe(_ =>
            {
                AcknowledgeMessagesCount++;

                Logger?.LogDebug($"Message acknowledged");

                var isMessageBatchAcknowledged = messages.All(message => message.IsAcknowledged);

                if (isMessageBatchAcknowledged)
                {
                    if (!taskCompletionSource.Task.IsCompleted)
                    {
                        taskCompletionSource.SetResult();

                        Logger?.LogDebug($"Finalized batch of {messages.Length} acknowledged");
                    }
                }

            });

            SendInternal(messages, timeout.Value);

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

                var candidate = _connectedBus.Values.FirstOrDefault(bus => bus as TBus != null);

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
            _cancellationTokenSource.Cancel();

            _cleanUp.Dispose();

            foreach (var pid in _rootPidRegistry)
            {
                RootContext.Stop(pid);
            }

            ActorSystem.ShutdownAsync($"Disposing {nameof(System.ProtoActorSystem)}").Wait();

        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (ActorSystem.Shutdown.IsCancellationRequested)
            {
                return HealthCheckResult.Unhealthy();
            }

            var healthCheckDescription = $"{Id} HealthChecks";

            if (_connectedBus.Count == 0) return new HealthCheckResult(HealthStatus.Healthy, healthCheckDescription);

            Exception? exception = null;

            var healthChecksResults = Array.Empty<HealthCheckResult>();
            var healthStatus = HealthStatus.Healthy;
            var data = new Dictionary<string, object>();

            try
            {
                healthChecksResults = await Task.WhenAll(_connectedBus.Select(bus => bus.Value.CheckHealthAsync(context)));
                healthStatus = healthChecksResults.Select(anabasisHealthCheck => anabasisHealthCheck.Status).Min();

                foreach (var anabasisHealthCheck in healthChecksResults.SelectMany(anabasisHealthCheck => anabasisHealthCheck.Data))
                {
                    data.Add(anabasisHealthCheck.Key, anabasisHealthCheck.Value);
                }
            }

            catch (Exception ex)
            {
                healthStatus = HealthStatus.Unhealthy;
                exception = ex.GetActualException();
            }

            if (_protoActorPoolDispatchQueue.IsFaulted)
            {
                exception = _protoActorPoolDispatchQueue.LastError;
                healthStatus = HealthStatus.Unhealthy;
                data.Add($"{nameof(System.ProtoActorSystem)} is in a faulted state", _protoActorPoolDispatchQueue.LastError?.Message);
            }

            return new HealthCheckResult(healthStatus, healthCheckDescription, exception, data);

        }

        public TActor GetSystemSpawnActor<TActor>(PID pid) where TActor : class, IActor
        {
            var actorContextAndPid = _spawnedActorContext.FirstOrDefault(spawnActor => spawnActor.pid == pid);

            if (default == actorContextAndPid)
            {
                throw new InvalidOperationException($"Cannot found actor with PID {pid} at the system level");
            }

            if (actorContextAndPid.actorContext.Actor is TActor tActor)
            {
                return actorContextAndPid.actorContext.Actor as TActor;
            }
            else
            {
                throw new InvalidOperationException($"Actor with PID {pid} is of type {actorContextAndPid.actorContext.Actor.GetType()} and not {typeof(TActor)}");
            }

        }
    }
}
