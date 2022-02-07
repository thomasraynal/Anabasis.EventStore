using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Anabasis.Common
{
    //adapted from https://github.com/Abc-Arbitrage/Zebus/blob/master/src/Abc.Zebus/Util/Collections/FlushableBlockingCollection.cs
    public class FlushableBlockingCollection<T> : IDisposable
    {
        private readonly ManualResetEventSlim _addSignal = new();
        private volatile ConcurrentQueue<T> _queue = new();
        private volatile bool _isAddingCompleted;
        private ManualResetEventSlim? _isEmptySignal;
        private int _hasChangedSinceLastWaitForEmpty;
        private readonly int _bachSize;
        private readonly int _queueMaxSize;

        public FlushableBlockingCollection(int bachSize, int queueMaxSize)
        {
            _bachSize = bachSize;
            _queueMaxSize = queueMaxSize;
        }

        public int Count => _queue.Count;

        public bool CanAdd => _queue.Count < _queueMaxSize && !_isAddingCompleted;

        public void CompleteAdding()
        {
            _isAddingCompleted = true;
            _addSignal.Set();
        }

        public void Add(T item)
        {
            if (_isAddingCompleted)
                throw new InvalidOperationException("Adding completed");

            if (!CanAdd)
                throw new InvalidOperationException($"Memory queue max size of {_queueMaxSize} is reached - cannot add");

            _hasChangedSinceLastWaitForEmpty = 1;
            _queue.Enqueue(item);
            _addSignal.Set();
        }

        public IEnumerable<List<T>> GetConsumingEnumerable()
        {

            var items = new List<T>(_bachSize);

            while (!IsAddingCompletedAndEmpty)
            {
                if (_queue.TryDequeue(out var item))
                {
                    _hasChangedSinceLastWaitForEmpty = 1;

                    items.Clear();
                    items.Add(item);

                    while (items.Count < _bachSize && _queue.TryDequeue(out item))
                        items.Add(item);

                    yield return items;
                }
                else
                {
                    _isEmptySignal?.Set();

                    // a longer wait timeout decreases CPU usage and improves latency
                    // but the guy who wrote this code is not comfortable with long timeouts in waits or sleeps
                    if (_addSignal.Wait(200))
                        _addSignal.Reset();
                }
            }
        }

        private bool IsAddingCompletedAndEmpty => _isAddingCompleted && _queue.Count == 0;

        public void Dispose()
        {
            CompleteAdding();
        }

        public ConcurrentQueue<T> Flush()
        {
            var items = _queue;

            _queue = new ConcurrentQueue<T>();
            _addSignal.Set();

            return items;
        }

        public bool WaitUntilIsEmpty()
        {
            var signal = _isEmptySignal;

            if (signal == null)
            {
                signal = new ManualResetEventSlim();
                var prevSignal = Interlocked.CompareExchange(ref _isEmptySignal, signal, null);
                signal = prevSignal ?? signal;
            }

            signal.Reset();
            _addSignal.Set();
            signal.Wait();

            return Interlocked.Exchange(ref _hasChangedSinceLastWaitForEmpty, 0) != 0;
        }
    }
}
