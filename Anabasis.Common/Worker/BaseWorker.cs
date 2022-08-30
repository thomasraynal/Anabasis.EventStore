﻿using Anabasis.Common.Configuration;
using Anabasis.Common.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public abstract class BaseWorker : IWorker
    {
        private CompositeDisposable _cleanUp;
        private Dictionary<Type, IBus> _connectedBus;
        private MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private IWorkerMessageDispatcherStrategy _workerMessageDispatcherStrategy;
        private IWorkerConfiguration _workerConfiguration;
        private WorkerDispatchQueue[] _workerDispatchQueues;

        public string Id { get; private set; }
        public ILogger? Logger { get; private set; }

#nullable disable

        protected BaseWorker(IWorkerConfigurationFactory workerConfigurationFactory,
            IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy = null,
            ILoggerFactory loggerFactory = null)
        {
            Setup(workerConfigurationFactory.GetConfiguration(GetType()), workerMessageDispatcherStrategy, loggerFactory);
        }

        protected BaseWorker(IWorkerConfiguration workerConfiguration,
            IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy = null,
            ILoggerFactory loggerFactory = null)
        {
            Setup(workerConfiguration, workerMessageDispatcherStrategy, loggerFactory);
        }

#nullable enable

        private void Setup(IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy? workerMessageDispatcherStrategy, ILoggerFactory? loggerFactory)
        {
           
            Id = $"{GetType().Name}-{Guid.NewGuid()}";
            Logger = loggerFactory?.CreateLogger(GetType());

            _cleanUp = new CompositeDisposable();
            _connectedBus = new Dictionary<Type, IBus>();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
            _workerMessageDispatcherStrategy = workerMessageDispatcherStrategy ?? new RoundRobinDispatcherStrategy();
            _workerConfiguration = workerConfiguration;

            var dispachQueueConfiguration = new WorkerDispatchQueueConfiguration(Handle, workerConfiguration.CrashAppOnFailure)
            {
                MessageBufferMaxSize = workerConfiguration.MessageBufferMaxSize,
                MessageBufferAbsoluteTimeoutInSecond = workerConfiguration.MessageBufferAbsoluteTimeoutInSecond,
                MessageBufferSlidingTimeoutInSecond = workerConfiguration.MessageBufferSlidingTimeoutInSecond
            };

            _workerDispatchQueues = Enumerable.Range(0, workerConfiguration.DispatcherCount).Select(_ =>
            {
                return new WorkerDispatchQueue(Id, dispachQueueConfiguration, loggerFactory: loggerFactory);

            }).ToArray();

            _workerMessageDispatcherStrategy.Initialize(_workerDispatchQueues);

        }

        public virtual bool IsConnected => _connectedBus.Values.All(bus => bus.ConnectionStatusMonitor.IsConnected);

        public bool IsFaulted
        {
            get
            {
                return _workerDispatchQueues.Any(workerDispatchQueue => workerDispatchQueue.IsFaulted);
            }
        }

        public Exception? LastError
        {
            get
            {
                var workerDispatchQueue = _workerDispatchQueues.FirstOrDefault(workerDispatchQueue => null != workerDispatchQueue.LastError);

                return workerDispatchQueue?.LastError;
            }
        }

        public virtual Task OnError(IEvent source, Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();

            return Task.CompletedTask;
        }

        public override bool Equals(object obj)
        {
            return obj is BaseWorker worker &&
                   Id == worker.Id;
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

            while (!IsConnected && DateTime.UtcNow > waitUntilMax)
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

            Exception? exception = null;

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

            if (IsFaulted)
            {
                exception = this.LastError;
                healthStatus = HealthStatus.Unhealthy;
                data.Add("Worker is in a faulted state", LastError?.Message);
            }

            return new HealthCheckResult(healthStatus, healthCheckDescription, exception, data);

        }

        public virtual Task OnInitialized()
        {
            return Task.CompletedTask;
        }

        public Task Handle(IMessage[] messages)
        {
            return Task.CompletedTask;
        }

        public abstract Task Handle(IEvent[] messages);
    }
}
