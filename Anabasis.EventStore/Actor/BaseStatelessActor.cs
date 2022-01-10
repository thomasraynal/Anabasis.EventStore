using Anabasis.EventStore.Event;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.Common;
using System.Linq;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseStatelessActor : IDisposable, IStatelessActor
    {
        private readonly Dictionary<Guid, TaskCompletionSource<ICommandResponse>> _pendingCommands;
        private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private readonly CompositeDisposable _cleanUp;
        private readonly IEventStoreRepository _eventStoreRepository;

        private readonly List<IEventStoreStream> _eventStoreStreams;

        private readonly Dictionary<Type, IBus> _connectedBus;

        public ILogger Logger { get; }

        protected BaseStatelessActor(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null)
        {
            Id = $"{GetType()}-{Guid.NewGuid()}";

            _cleanUp = new CompositeDisposable();
            _eventStoreRepository = eventStoreRepository;
            _pendingCommands = new Dictionary<Guid, TaskCompletionSource<ICommandResponse>>();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
            _eventStoreStreams = new List<IEventStoreStream>();
            _connectedBus = new Dictionary<Type, IBus>();

            Logger = loggerFactory?.CreateLogger(GetType());

        }

        public string Id { get; }

        public bool IsConnected => _eventStoreRepository.IsConnected;

        public void SubscribeToEventStream(IEventStoreStream eventStoreStream, bool closeSubscriptionOnDispose = false)
        {
            eventStoreStream.Connect();

            Logger?.LogDebug($"{Id} => Subscribing to {eventStoreStream.Id}");

            _eventStoreStreams.Add(eventStoreStream);

            var disposable = eventStoreStream.OnEvent().Subscribe(async @event => await OnEventReceived(@event));

            if (closeSubscriptionOnDispose)
            {
                _cleanUp.Add(eventStoreStream);
            }

            _cleanUp.Add(disposable);
        }

        public virtual Task OnError(IEvent source, Exception exception)
        {
            return Task.CompletedTask;
        }

        public async Task EmitEventStore<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent: IEvent
        {
            if (!_eventStoreRepository.IsConnected) throw new InvalidOperationException("Not connected");

            Logger?.LogDebug($"{Id} => Emitting {@event.EntityId} - {@event.GetType()}");

            await _eventStoreRepository.Emit(@event, extraHeaders);
        }

        public Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {
            Logger?.LogDebug($"{Id} => Sending command {command.EntityId} - {command.GetType()}");

            var taskSource = new TaskCompletionSource<ICommandResponse>();

            var cancellationTokenSource = null != timeout ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

            cancellationTokenSource.Token.Register(() => taskSource.TrySetCanceled(), false);

            _pendingCommands[command.EventID] = taskSource;

            _eventStoreRepository.Emit(command);

            return taskSource.Task.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully) return (TCommandResult)task.Result;
                if (task.IsCanceled) throw new Exception("Command went in timeout");

                throw task.Exception;

            }, cancellationTokenSource.Token);

        }

        public async Task OnEventReceived(IEvent @event)
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

            while(!IsConnected ||  DateTime.UtcNow > waitUntilMax)
            {
                await Task.Delay(100);
            }

            if (!IsConnected) throw new InvalidOperationException("Unable to connect");
        }

        public TBus GetConnectedBus<TBus>() where TBus: class
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
