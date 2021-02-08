using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{
  //todo: should be flushbable on dispose
  public abstract class DispatchQueue<TMessage>: IDisposable
  {

    private readonly Task _workProc;
    private readonly BlockingCollection<TMessage> _workQueue;

    protected DispatchQueue()
    {
      _workQueue = new BlockingCollection<TMessage>();
      _workProc = Task.Run(HandleWork, CancellationToken.None);
    }

    public abstract Task OnMessageReceived(TMessage message);

    public void Push(TMessage message)
    {
      _workQueue.Add(message);
    }

    private void HandleWork()
    {
      foreach (var message in _workQueue.GetConsumingEnumerable())
      {
        OnMessageReceived(message);
      }
    }

    public void Dispose()
    {
      _workQueue.Dispose();
      _workProc.Dispose();
    }
  }
}
