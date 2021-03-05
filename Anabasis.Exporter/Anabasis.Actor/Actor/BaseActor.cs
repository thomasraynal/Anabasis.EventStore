using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Queue;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Actor
{
  public abstract class BaseActor : IDisposable
  {
    private readonly Dictionary<Guid, TaskCompletionSource<ICommandResponse>> _pendingCommands;
    private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
    private readonly CompositeDisposable _cleanUp;
    private readonly IEventStoreRepository _eventStoreRepository;

    protected BaseActor(IEventStoreRepository eventStoreRepository)
    {
      Id = $"{GetType()}-{Guid.NewGuid()}";

      _cleanUp = new CompositeDisposable();
      _eventStoreRepository = eventStoreRepository;
      _pendingCommands = new Dictionary<Guid, TaskCompletionSource<ICommandResponse>>();
      _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
    }

    public string Id { get; }

    public void SubscribeTo(IEventStoreQueue eventStoreQueue)
    {
      var disposable = eventStoreQueue.OnEvent().Subscribe(async @event => await OnEventReceived(@event));

      _cleanUp.Add(disposable);
    }

    public void Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders)
    {
      _eventStoreRepository.Emit(@event, extraHeaders);
    }

    public Task Send<TCommandResult>(ICommand command, TimeSpan? timeout) where TCommandResult : ICommandResponse
    {

      throw new NotImplementedException();

      var taskSource = new TaskCompletionSource<ICommandResponse>();

      var cancellationTokenSource = null != timeout ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

      cancellationTokenSource.Token.Register(() => taskSource.TrySetCanceled(), false);

      _pendingCommands[command.EventID] = taskSource;

      //Mediator.Emit(command);

      return taskSource.Task.ContinueWith(task =>
      {
        if (task.IsCompletedSuccessfully) return (TCommandResult)task.Result;
        if (task.IsCanceled) throw new Exception("Command went in timeout");

        throw task.Exception;

      }, cancellationTokenSource.Token);

    }

    private async Task OnEventReceived(IEvent @event)
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
        await (Task)candidateHandler.Invoke(this, new object[] { @event });
      }
    }

    public override bool Equals(object obj)
    {
      return obj is BaseActor actor &&
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
