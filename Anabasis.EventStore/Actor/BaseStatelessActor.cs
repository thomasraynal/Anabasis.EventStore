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
        private readonly ILogger _logger;
        private readonly IEventStoreRepository _eventStoreRepository;

        protected BaseStatelessActor(IEventStoreRepository eventStoreRepository, ILogger logger = null)
        {
            Id = $"{GetType()}-{Guid.NewGuid()}";

            _cleanUp = new CompositeDisposable();
            //_logger = logger;
            _eventStoreRepository = eventStoreRepository;
            _pendingCommands = new Dictionary<Guid, TaskCompletionSource<ICommandResponse>>();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
        }

        public string Id { get; }

        public void SubscribeTo(IEventStoreQueue eventStoreQueue, bool closeSubscriptionOnDispose = false)
        {
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

            await _eventStoreRepository.Emit(@event, extraHeaders);
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {

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
    }
}
