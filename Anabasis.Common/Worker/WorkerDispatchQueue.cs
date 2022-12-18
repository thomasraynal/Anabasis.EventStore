using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Disposables;
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
        private readonly ManualResetEventSlim _addSignal = new();

        public long ProcessedMessagesCount { get; private set; }
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

            Logger?.LogDebug($"{Id} started");

        }

        public IMessage[] TryEnqueue(IMessage[] messages, out IMessage[] unProcessedMessages)
        {
            var enqueuedMessages = _queueBuffer.TryEnqueue(messages, out unProcessedMessages);

            if (enqueuedMessages.Length > 0)
            {
                _addSignal.Set();
            }

            return enqueuedMessages;
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

                    var hasPull = _queueBuffer.TryPull(out messageBatch);

                    if (hasPull)
                    {

                        var events = messageBatch.Select(message => message.Content).ToArray();

                        await _workerDispatchQueueConfiguration.OnEventsReceived(events);

                        foreach (var message in messageBatch)
                        {
                            await message.Acknowledge();
                        }

                        ProcessedMessagesCount += messageBatch.Length;

                    }
                    else
                    {
                        if (_addSignal.Wait(200))
                        {
                            _addSignal.Reset();
                        }

                    }

                }
                catch (Exception exception)
                {
                    exception.Data["messages"] = messageBatch.ToJson();

                    Logger?.LogError(exception, $"An exception occured during the message consumption process");

                    this.LastError = exception;

                    var unacknowledgeMessageTask = messageBatch.Where(message => !message.IsAcknowledged).Select(message => message.NotAcknowledge());

                    await Task.WhenAll(unacknowledgeMessageTask);

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

        public bool CanPush
        {
            get
            {
                return _queueBuffer.CanPush;
            }
        }

        public void Dispose()
        {
            _queueBuffer.Dispose();
            _thread?.Join();
        }
    }
}
