using Anabasis.Common.Contracts;
using Anabasis.Common.Queue;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public class WorkerDispatchQueue : IWorkerDispatchQueue
    {
        private readonly IQueueBuffer _queueBuffer;
        private readonly IKillSwitch _killSwitch;
        private readonly Thread _thread;
        private readonly CompositeDisposable _cleanUp;

        public Exception? LastError { get; private set; }
        public bool IsFaulted { get; private set; }
        public ILogger? Logger { get; }
        public string Owner { get; }
        public string Id { get; }

        public WorkerDispatchQueue(string ownerId,
            WorkerDispatchQueueConfiguration workerDispatchQueueConfiguration,
            IQueueBuffer? queueBuffer = null,
            ILoggerFactory? loggerFactory = null,
            IKillSwitch? killSwitch = null)
        {

            Logger = loggerFactory?.CreateLogger(GetType());
            Owner = ownerId;
            Id = $"{nameof(DispatchQueue)}_{ownerId}_{Guid.NewGuid()}";

            _killSwitch = killSwitch ?? new KillSwitch();

            _queueBuffer = queueBuffer ?? new SimpleQueueBuffer(
                    workerDispatchQueueConfiguration.MessageBufferMaxSize,
                    workerDispatchQueueConfiguration.MessageBufferAbsoluteTimeoutInSecond,
                    workerDispatchQueueConfiguration.MessageBufferSlidingTimeoutInSecond);

            _cleanUp = new CompositeDisposable();

            _thread = new Thread(HandleWork)
            {
                IsBackground = true,
                Name = Id,
            };

            _thread.Start();

            Logger?.LogDebug("{0} started", Id);

        }
        public void Enqueue(IMessage message)
        {

        }

        private async void HandleWork()
        {
            
        }
        public bool CanEnqueue()
        {
            return _queueBuffer.CanAdd;
        }

        public async ValueTask DisposeAsync()
        {
            await _queueBuffer.DisposeAsync();
            _thread?.Join();
        }
    }
}
