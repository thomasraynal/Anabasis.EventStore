using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Stream
{
    public abstract class DispatchStream<TMessage> : IDisposable, IDispatchStream<TMessage>
    {

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly BlockingCollection<TMessage> _workStream;
        private readonly Task _workProc;

        protected DispatchStream()
        {
            _workStream = new BlockingCollection<TMessage>();
            _workProc = Task.Run(HandleWork, CancellationToken.None);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        protected abstract Task OnMessageReceived(TMessage message);

        public void Enstream(TMessage message)
        {
            _workStream.Add(message);
        }

        private void HandleWork()
        {
            foreach (var message in _workStream.GetConsumingEnumerable(_cancellationTokenSource.Token))
            {
                OnMessageReceived(message).Wait();
            }
        }
        private void WaitUntilIsEmpty()
        {
            _workStream.CompleteAdding();

            while (_workStream.Count > 0)
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
