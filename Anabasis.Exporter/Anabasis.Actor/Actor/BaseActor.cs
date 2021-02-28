using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Actor
{
  public abstract class BaseActor : DispatchQueue<IActorEvent>, IActor
  {
    private readonly Dictionary<Guid, TaskCompletionSource<ICommandResponse>> _pendingCommands;

    protected BaseActor(IMediator simpleMediator)
    {

      Mediator = simpleMediator;

      ActorId = $"{GetType()}-{Guid.NewGuid()}";

      _pendingCommands = new Dictionary<Guid, TaskCompletionSource<ICommandResponse>>();
      _messageHandlerInvokerCache = new MessageHandlerInvokerCache();

    }

    public IMediator Mediator { get; }

    private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;

    public abstract string StreamId { get; }

    public string ActorId { get; }

    protected async override Task OnMessageReceived(IActorEvent @event)
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

    public Task Send<TCommandResult>(ICommand command, TimeSpan? timeout) where TCommandResult : ICommandResponse
    {

      var taskSource = new TaskCompletionSource<ICommandResponse>();

      var cancellationTokenSource = null != timeout ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

      cancellationTokenSource.Token.Register(() => taskSource.TrySetCanceled(), false);

      _pendingCommands[command.EventID] = taskSource;

      Mediator.Emit(command);

      return taskSource.Task.ContinueWith(task =>
      {
        if (task.IsCompletedSuccessfully) return (TCommandResult)task.Result;
        if (task.IsCanceled) throw new Exception("timeout");

        throw task.Exception;

      }, cancellationTokenSource.Token);

    }

    public bool CanConsume(IActorEvent @event)
    {
      return null != _messageHandlerInvokerCache.GetMethodInfo(GetType(), @event.GetType());
    }

    public override bool Equals(object obj)
    {
      return obj is BaseActor actor &&
             ActorId == actor.ActorId;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(ActorId);
    }
  }
}
