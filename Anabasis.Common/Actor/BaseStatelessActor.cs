﻿using Anabasis.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public abstract class BaseStatelessActor :  IActor
    {
        protected readonly Dictionary<Guid, TaskCompletionSource<ICommandResponse>> _pendingCommands;
        private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private readonly CompositeDisposable _cleanUp;
        private readonly Dictionary<Type, IBus> _connectedBus;
        private readonly IActorConfiguration _actorConfiguration;
        private readonly IDispatchQueue<IEvent> _dispatchQueue;

        public ILogger Logger { get; }

        protected BaseStatelessActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null)
        {
            Id = $"{GetType()}-{Guid.NewGuid()}";

            _cleanUp = new CompositeDisposable();
            _pendingCommands = new Dictionary<Guid, TaskCompletionSource<ICommandResponse>>();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
            _connectedBus = new Dictionary<Type, IBus>();
            _actorConfiguration = actorConfiguration;
            _dispatchQueue = new DispatchQueue<IEvent>(OnEventReceivedInternal, 
                _actorConfiguration.ActorMailBoxMessageBatchSize,
                _actorConfiguration.ActorMailBoxMessageMessageQueueMaxSize);

            Logger = loggerFactory?.CreateLogger(GetType());

        }

        public string Id { get; }

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

                    if (_pendingCommands.ContainsKey(commandResponse.CommandId))
                    {

                        var task = _pendingCommands[commandResponse.CommandId];

                        task.SetResult(commandResponse);

                        _pendingCommands.Remove(commandResponse.EventID, out _);
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

        public void ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false)
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
        }

        public void AddDisposable(IDisposable disposable)
        {
            _cleanUp.Add(disposable);
        }
    }
}
