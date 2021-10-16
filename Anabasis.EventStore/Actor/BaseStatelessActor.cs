using Anabasis.EventStore.Event;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseStatelessActor : IDisposable, IStatelessActor
    {
        private readonly Dictionary<Guid, TaskCompletionSource<ICommandResponse>> _pendingCommands;
        private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private readonly CompositeDisposable _cleanUp;
        private readonly IEventStoreRepository _eventStoreRepository;

        public ILogger Logger { get; }

        protected BaseStatelessActor(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null)
        {
            Id = $"{GetType()}-{Guid.NewGuid()}";

            _cleanUp = new CompositeDisposable();
            _eventStoreRepository = eventStoreRepository;
            _pendingCommands = new Dictionary<Guid, TaskCompletionSource<ICommandResponse>>();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
            
            Logger = loggerFactory?.CreateLogger(GetType());

        }

        public string Id { get; }

        public bool IsConnected => _eventStoreRepository.IsConnected;

        public void SubscribeTo(IEventStoreQueue eventStoreQueue, bool closeSubscriptionOnDispose = false)
        {
            eventStoreQueue.Connect();

            Logger?.LogDebug($"{Id} => Subscribing to {eventStoreQueue.Id}");

            var disposable = eventStoreQueue.OnEvent().Subscribe(async @event => await OnEventReceived(@event));

            if (closeSubscriptionOnDispose)
            {
                _cleanUp.Add(eventStoreQueue);
            }

            _cleanUp.Add(disposable);
        }

        public virtual Task OnError(IEvent source, Exception exception)
        {
            return Task.CompletedTask;
        }

        public async Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders)
        {
            if (!_eventStoreRepository.IsConnected) throw new InvalidOperationException("Not connected");

            Logger?.LogDebug($"{Id} => Emitting {@event.StreamId} - {@event.GetType()}");

            await _eventStoreRepository.Emit(@event, extraHeaders);
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {
            Logger?.LogDebug($"{Id} => Sending command {command.StreamId} - {command.GetType()}");

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

        private async Task OnEventReceived(IEvent @event)
        {
            try
            {
                Logger?.LogDebug($"{Id} => Receiving event {@event.StreamId} - {@event.GetType()}");

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
    }
}
