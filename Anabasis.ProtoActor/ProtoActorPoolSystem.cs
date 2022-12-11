using Anabasis.Common;
using Proto;
using Proto.DependencyInjection;
using Proto.Router;
using System.Reactive.Disposables;

namespace Anabasis.ProtoActor
{
    public class ProtoActorPoolSystem : IProtoActorSystem
    {

        private readonly ISupervisorStrategy _supervisorStrategy;
        private readonly ISupervisorStrategy? _chidSupervisorStrategy;
        private readonly Dictionary<Type, IBus> _connectedBus;
        private readonly CompositeDisposable _cleanUp;
        private readonly List<PID> _registry;

        public ActorSystem ActorSystem { get; }
        public RootContext RootContext { get; }



        public ProtoActorPoolSystem(ISupervisorStrategy supervisorStrategy, IServiceProvider serviceProvider, ISupervisorStrategy? chidSupervisorStrategy = null)
        {
            _supervisorStrategy = supervisorStrategy;
            _chidSupervisorStrategy = chidSupervisorStrategy ?? supervisorStrategy;
            _connectedBus = new Dictionary<Type, IBus>();
            _cleanUp = new CompositeDisposable();
            _registry = new List<PID>();

            ActorSystem = new ActorSystem().WithServiceProvider(serviceProvider);
            RootContext = new RootContext(ActorSystem);
        }

        public PID CreateRoundRobinPool<TActor>(int poolSize, Action<Props>? onCreateProps = null) where TActor : IActor
        {
            var props = CreateCommonProps<TActor>(onCreateProps);

            var newRoundRobinPoolProps = RootContext.NewRoundRobinPool(props, poolSize);

            var pid = RootContext.Spawn(newRoundRobinPoolProps);

            _registry.Add(pid);

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
            var props = CreateCommonProps<TActor>(onCreateProps);

            var consistentHashPoolProps = RootContext.NewConsistentHashPool(props, poolSize, hash, replicaCount, messageHasher);

            var pid = RootContext.Spawn(consistentHashPoolProps);

            _registry.Add(pid);

            return pid;
        }

        public Task OnMessageReceived(IMessage message)
        {
            foreach (var pid in _registry)
            {
                RootContext.Send(pid, message);
            }

            return Task.CompletedTask;
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
        }
    }
}
