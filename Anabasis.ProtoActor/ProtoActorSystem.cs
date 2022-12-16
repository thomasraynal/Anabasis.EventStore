using Anabasis.Common;
using Anabasis.Common.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public class ProtoActorSystem : IProtoActorSystem
    {
        private readonly ISupervisorStrategy _supervisorStrategy;
        private readonly ISupervisorStrategy? _chidSupervisorStrategy;
        private readonly Dictionary<Type, IBus> _connectedBus;
        private readonly CompositeDisposable _cleanUp;

        public ProtoActorSystem(ISupervisorStrategy supervisorStrategy,
            IServiceProvider serviceProvider,
            ILoggerFactory? loggerFactory = null,
            ISupervisorStrategy? chidSupervisorStrategy = null,
            IKillSwitch? killSwitch = null)
        {
            _supervisorStrategy = supervisorStrategy;
            _chidSupervisorStrategy = chidSupervisorStrategy ?? supervisorStrategy;
            _connectedBus = new Dictionary<Type, IBus>();
            _cleanUp = new CompositeDisposable();

            ActorSystem = new ActorSystem().WithServiceProvider(serviceProvider);
            RootContext = new RootContext(ActorSystem);
        }

        public ActorSystem ActorSystem { get; }
        public RootContext RootContext { get; }

        public ILogger? Logger { get; }
        public string Id { get; }


        public long ProcessedMessagesCount { get; private set; }
        public long ReceivedMessagesCount { get; private set; }
        public long AcknowledgeMessagesCount { get; private set; }
        public long EnqueuedMessagesCount { get; private set; }

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

        public PID CreateActor<TActor>() where TActor : IActor
        {
            return null;
            //var props = CreateCommonProps<TActor>(onCreateProps);
        }

        public Task Send(IMessage message, TimeSpan? timeout = null)
        {
           // RootContext.Send(pid, message);

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
