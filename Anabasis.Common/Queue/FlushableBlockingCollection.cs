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
        private readonly int _bachSize;
        private readonly int _queueMaxSize;

        public FlushableBlockingCollection(int bachSize, int queueMaxSize)
        {
            _bachSize = bachSize;
            _queueMaxSize = queueMaxSize;
        }

        public int Count => _queue.Count;

        public bool CanAdd => _queue.Count < _queueMaxSize && !_isAddingCompleted;

        public void Add(T item)
        {
            if (_isAddingCompleted)
                throw new InvalidOperationException("Adding completed");

            if (!CanAdd)
                throw new InvalidOperationException($"Memory queue max size of {_queueMaxSize} is reached - cannot add");

            _queue.Enqueue(item);
            _addSignal.Set();
        }

        public void Add(T[] items)
        {
            foreach(var item in items)
            {
                Add(item);
            }
        }

        public IEnumerable<List<T>> GetConsumingEnumerable()
        {

            var items = new List<T>(_bachSize);

            while (!IsAddingCompletedAndEmpty)
            {
                if (_queue.TryDequeue(out var item))
                {

                    items.Clear();
                    items.Add(item);

                    while (items.Count < _bachSize && _queue.TryDequeue(out item))
                        items.Add(item);

                    yield return items;
                }
                else
                {
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
            SetAddingCompleted();
            _addSignal.Set();
        }

        public void SetAddingCompleted()
        {
            _isAddingCompleted = true;
        }

        public ConcurrentQueue<T> Flush()
        {
            var items = _queue;

            _queue = new ConcurrentQueue<T>();
            _addSignal.Set();

            return items;
        }

    }
}
