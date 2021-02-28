using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Actor
{
  public abstract class DispatchQueue<TMessage> : IDisposable, IDispatchQueue<TMessage>
  {

    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly BlockingCollection<TMessage> _workQueue;
    private readonly Task _workProc;

    protected DispatchQueue()
    {
      _workQueue = new BlockingCollection<TMessage>();
      _workProc = Task.Run(HandleWork, CancellationToken.None);
      _cancellationTokenSource = new CancellationTokenSource();
    }

    protected abstract Task OnMessageReceived(TMessage message);

    public void Enqueue(TMessage message)
    {
      _workQueue.Add(message);
    }

    private void HandleWork()
    {
      foreach (var message in _workQueue.GetConsumingEnumerable(_cancellationTokenSource.Token))
      {
        OnMessageReceived(message).Wait();
      }
    }
    private void WaitUntilIsEmpty()
    {
      _workQueue.CompleteAdding();

      while (_workQueue.Count > 0)
      {
        Thread.Sleep(1);
      }

      _cancellationTokenSource.Cancel();

      _workProc.Dispose();

    }

    public void Dispose()
    {
      WaitUntilIsEmpty();
    }
  }
}
