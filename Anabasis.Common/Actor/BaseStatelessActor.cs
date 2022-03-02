using Anabasis.Common.Actor;
using Anabasis.Common.Configuration;
using Anabasis.Common.Queue;
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
        private IDispatchQueue _dispatchQueue;

        public string Id { get; private set; }
        public bool IsDisposed { get; private set; }
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

            var dispachQueueConfiguration = new DispatchQueueConfiguration(
                OnEventReceivedInternal,
                _actorConfiguration.ActorMailBoxMessageBatchSize,
                _actorConfiguration.ActorMailBoxMessageMessageQueueMaxSize,
               _actorConfiguration.CrashAppOnError
            );

            _dispatchQueue = new DispatchQueue(dispachQueueConfiguration, loggerFactory);

            _cleanUp.Add(_dispatchQueue);

            Logger = loggerFactory?.CreateLogger(GetType());
        }

        public virtual bool IsConnected => _connectedBus.Values.All(bus => bus.ConnectionStatusMonitor.IsConnected);

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

                if (null != candidateHandler)
                {
                    ((Task)candidateHandler.Invoke(this, new object[] { @event })).Wait();
                }

            }
            catch (Exception exception)
            {
                await OnError(@event, exception);
            }
        }

        public void OnMessageReceived(IMessage @event, TimeSpan? timeout = null)
        {
             timeout = timeout == null ? TimeSpan.FromMinutes(30) : timeout.Value;

            if(!_dispatchQueue.CanEnqueue())
            {
                SpinWait.SpinUntil(() => _dispatchQueue.CanEnqueue(), (int)timeout.Value.TotalMilliseconds);

                if(!_dispatchQueue.CanEnqueue())
                    throw new TimeoutException("Unable to process event - timeout reached");
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

            IsDisposed = true;
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
                    throw new InvalidOperationException($"Bus of type {busType} is not registered");
                }

                return (TBus)candidate;
            }

            return (TBus)_connectedBus[busType];
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

        public void AddDisposable(IDisposable disposable)
        {
            _cleanUp.Add(disposable);
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var healthCheckDescription = $"{Id} HealthChecks";

            if (_connectedBus.Count == 0) return new HealthCheckResult(HealthStatus.Healthy, healthCheckDescription);

            Exception exception = null;

            var healthChecksResults = new HealthCheckResult[0];
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
                exception = ex;
            }

            return new HealthCheckResult(healthStatus, healthCheckDescription, exception, data);

        }

        public virtual Task OnInitialized()
        {
            return Task.CompletedTask;
        }
    }
}
