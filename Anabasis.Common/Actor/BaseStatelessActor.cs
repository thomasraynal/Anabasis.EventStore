using Anabasis.Common.Actor;
using Anabasis.Common.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public abstract class BaseStatelessActor : IActor
    {

        private MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private CompositeDisposable _cleanUp;
        private Dictionary<Type, IBus> _connectedBus;
        private IActorConfiguration _actorConfiguration;
        private IDispatchQueue<IEvent> _dispatchQueue;

        public string Id { get; private set; }
        protected Dictionary<Guid, TaskCompletionSource<ICommandResponse>> PendingCommands { get; private set; }
        public ILogger Logger { get; private set; }

        protected BaseStatelessActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null)
        {
            Setup(actorConfigurationFactory.GetConfiguration(GetType()), loggerFactory);
        }

        protected BaseStatelessActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null)
        {
            Setup(actorConfiguration, loggerFactory);
        }

        private void Setup(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory)
        {
            Id = $"{GetType().Name}-{Guid.NewGuid()}";

            _cleanUp = new CompositeDisposable();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
            _connectedBus = new Dictionary<Type, IBus>();
            _actorConfiguration = actorConfiguration;
            _dispatchQueue = new DispatchQueue<IEvent>(OnEventReceivedInternal,
                _actorConfiguration.ActorMailBoxMessageBatchSize,
                _actorConfiguration.ActorMailBoxMessageMessageQueueMaxSize);

            PendingCommands = new Dictionary<Guid, TaskCompletionSource<ICommandResponse>>();
            Logger = loggerFactory?.CreateLogger(GetType());
        }

        public virtual bool IsConnected => _connectedBus.Values.All(bus => bus.IsConnected);

        public virtual Task OnError(IEvent source, Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();

            return Task.CompletedTask;
        }

        private async Task OnEventReceivedInternal(IEvent @event)
        {
            try
            {

                Logger?.LogDebug($"{Id} => Receiving event {@event.EntityId} - {@event.GetType()}");

                var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), @event.GetType());

                if (@event is ICommandResponse)
                {

                    var commandResponse = @event as ICommandResponse;

                    if (PendingCommands.ContainsKey(commandResponse.CommandId))
                    {

                        var task = PendingCommands[commandResponse.CommandId];

                        task.SetResult(commandResponse);

                        PendingCommands.Remove(commandResponse.EventID, out _);
                    }

                }
                else
                {
                    if (null != candidateHandler)
                    {
                        ((Task)candidateHandler.Invoke(this, new object[] { @event })).Wait();
                    }
                }

            }
            catch (Exception exception)
            {
                await OnError(@event, exception);
            }
        }

        public async Task OnEventReceived(IEvent @event, TimeSpan? timeout = null)
        {
            var timeoutDateTime = timeout == null ? DateTime.MaxValue : DateTime.UtcNow.Add(timeout.Value);

            while (!_dispatchQueue.CanEnqueue())
            {
                if (DateTime.UtcNow > timeoutDateTime)
                    throw new TimeoutException("Unable to process event - timout reached");

                //maybe think of something more optimized - no thread destack, some kind of event signaling?
                await Task.Delay(200);
            }

            _dispatchQueue.Enqueue(@event);
        }

        public override bool Equals(object obj)
        {
            return obj is BaseStatelessActor actor &&
                   Id == actor.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public virtual void Dispose()
        {
            _cleanUp.Dispose();
        }

        public async Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            if (IsConnected) return;

            var waitUntilMax = DateTime.UtcNow.Add(null == timeout ? Timeout.InfiniteTimeSpan : timeout.Value);

            while (!IsConnected || DateTime.UtcNow > waitUntilMax)
            {
                await Task.Delay(100);
            }

            if (!IsConnected) throw new InvalidOperationException("Unable to connect");
        }

        public TBus GetConnectedBus<TBus>() where TBus : class
        {
            var busType = typeof(TBus);

            if (!_connectedBus.ContainsKey(busType))
            {

                var candidate = _connectedBus.Values.FirstOrDefault(bus => (bus as TBus) != null);

                if (null == candidate)
                {
                    throw new InvalidOperationException($"Bus of type {busType} is already registered");
                }

                _connectedBus[busType] = candidate;
            }

            return (TBus)_connectedBus[busType];
        }

        public async Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false)
        {

            await bus.Initialize();

            var healthCheckResult = await bus.GetHealthCheck();

            if (healthCheckResult.Status == HealthStatus.Unhealthy)
                throw new BusUnhealthyException($"Bus {bus.BusId} is unhealthy", healthCheckResult);

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
        }

        public void AddDisposable(IDisposable disposable)
        {
            _cleanUp.Add(disposable);
        }

        //todo: add event store bus
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var healthCheckDescription = "Actor HealthChecks";

            if (_connectedBus.Count == 0) return new HealthCheckResult(HealthStatus.Healthy, healthCheckDescription);

            Exception exception = null;

            var anabasisHealthChecks = new HealthCheckResult[0];
            var healthStatus = HealthStatus.Healthy;
            var data = new Dictionary<string, object>();

            try
            {
                anabasisHealthChecks = await Task.WhenAll(_connectedBus.Select(bus => bus.Value.GetHealthCheck()));
                healthStatus = anabasisHealthChecks.Select(anabasisHealthCheck => anabasisHealthCheck.Status).Min();

                foreach (var anabasisHealthCheck in anabasisHealthChecks.SelectMany(anabasisHealthCheck => anabasisHealthCheck.Data))
                {
                    data.Add(anabasisHealthCheck.Key, anabasisHealthCheck.Value);
                }
            }

            catch (Exception ex)
            {
                healthStatus = HealthStatus.Unhealthy;
                exception = ex;
            }

            return new HealthCheckResult(healthStatus, healthCheckDescription, exception, data);

        }
    }
}
