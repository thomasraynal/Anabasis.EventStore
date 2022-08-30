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
        private readonly IWorkerDispatchQueueConfiguration _workerDispatchQueueConfiguration;
        private readonly CancellationToken _cancellationToken;
        private readonly Thread _thread;
        private readonly CompositeDisposable _cleanUp;

        public Exception? LastError { get; private set; }
        public bool IsFaulted { get; private set; }
        public ILogger? Logger { get; }
        public string Owner { get; }
        public string Id { get; }

        public WorkerDispatchQueue(string ownerId,
            IWorkerDispatchQueueConfiguration workerDispatchQueueConfiguration,
            CancellationToken cancellationToken,
            IQueueBuffer? queueBuffer = null,
            ILoggerFactory? loggerFactory = null,
            IKillSwitch? killSwitch = null)
        {

            Logger = loggerFactory?.CreateLogger(GetType());
            Owner = ownerId;
            Id = $"{nameof(DispatchQueue)}_{ownerId}_{Guid.NewGuid()}";

            _killSwitch = killSwitch ?? new KillSwitch();
            _workerDispatchQueueConfiguration = workerDispatchQueueConfiguration;

            _cancellationToken = cancellationToken;

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
        public void Push(IMessage message)
        {
            _queueBuffer.Push(message);
        }

        public void TryPush(IMessage[] messages, out IMessage[] unProcessedMessages)
        {
            _queueBuffer.TryPush(messages, out unProcessedMessages);
        }

        private async void HandleWork()
        {
           var messageBatch = Array.Empty<IMessage>();

            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (IsFaulted)
                    {
                        break;
                    }

                    messageBatch = _queueBuffer.Pull();

                    var events = messageBatch.Select(message => message.Content).ToArray();

                    await _workerDispatchQueueConfiguration.OnEventsReceived(events);

                    foreach (var message in messageBatch)
                    {
                        await message.Acknowledge();
                    }

                }
                catch (Exception exception)
                {
                    exception.Data["messages"] = messageBatch.ToJson();

                    Logger?.LogError(exception, $"An exception occured during the message consumption process");

                    this.LastError = exception;

                    foreach (var message in messageBatch)
                    {
                        await message.NotAcknowledge();
                    }

                    if (_workerDispatchQueueConfiguration.CrashAppOnError)
                    {
                        IsFaulted = true;

                        try
                        {
                            await _queueBuffer.Flush(true);
                        }
                        catch { }
                        finally
                        {
                            _killSwitch.KillProcess(exception);
                        }
                    }

                }

            }

            await _queueBuffer.Flush(true);
        }

        public bool CanPush()
        {
            return _queueBuffer.CanPush;
        }

        public void Dispose()
        {
            _queueBuffer.Dispose();

            _thread?.Join();
        }
    }
}
