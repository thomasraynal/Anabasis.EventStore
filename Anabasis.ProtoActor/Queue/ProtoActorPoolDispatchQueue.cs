using Anabasis.Common;
using Anabasis.Common.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Queue
{
    public class ProtoActorPoolDispatchQueue : IProtoActorPoolDispatchQueue
    {

        private readonly IProtoActorPoolDispatchQueueConfiguration _protoActorPoolDispatchQueueConfiguration;
        private readonly CompositeDisposable _cleanUp;
        private readonly Thread _thread;
        private readonly IQueueBuffer _queueBuffer;
        private readonly Action<IMessage[]> _messageHandler;
        private readonly CancellationToken _cancellationToken;
        private readonly IKillSwitch _killSwitch;
        private readonly ManualResetEventSlim _addSignal = new();

        public ProtoActorPoolDispatchQueue(string owner,
            IProtoActorPoolDispatchQueueConfiguration protoActorPoolDispatchQueueConfiguration,
            CancellationToken cancellationToken,
            Action<IMessage[]> messageHandler,
            IQueueBuffer? queueBuffer = null,
            ILoggerFactory? loggerFactory = null,
            IKillSwitch? killSwitch = null)
        {
            Owner = owner;
            Id = this.GetUniqueIdFromType();
            Logger = loggerFactory?.CreateLogger(GetType());

            _queueBuffer = queueBuffer ?? new SimpleQueueBuffer(protoActorPoolDispatchQueueConfiguration.MessageBufferMaxSize, 0, 0);
            _messageHandler = messageHandler;

            _cancellationToken = cancellationToken;

            _killSwitch = killSwitch ?? new KillSwitch();

            _protoActorPoolDispatchQueueConfiguration = protoActorPoolDispatchQueueConfiguration;

            _cleanUp = new CompositeDisposable();

            _thread = new Thread(HandleWork)
            {
                IsBackground = true,
                Name = Id,
            };

            _thread.Start();

   
        }

        public ILogger? Logger { get; }
        public string Owner { get; }
        public string Id { get; }

        public long ProcessedMessagesCount { get; private set; }
        public long PulledMessagesCount { get; private set; }
        public bool IsFaulted { get; private set; }

        public Exception? LastError { get; private set; }


        public IMessage[] TryEnqueue(IMessage[] messages, out IMessage[] unProcessedMessages)
        {
            var pushedMessages = _queueBuffer.TryEnqueue(messages, out unProcessedMessages);

            if (pushedMessages.Length > 0)
            {
                _addSignal.Set();
            }

            return pushedMessages;
        }

        private async void HandleWork()
        {

            Logger?.LogDebug($"{Id} started");

            var messageBatch = Array.Empty<IMessage>();

            while (!_cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (IsFaulted)
                    {
                        break;
                    }

                    var canPull = _queueBuffer.TryPull(out messageBatch);

                    if (canPull)
                    {
                        PulledMessagesCount += messageBatch.Length;

                        Logger?.LogDebug($"Pulled batch of {messageBatch.Length} messages");

                        _messageHandler(messageBatch);

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

                    LastError = exception;

                    var unacknowledgeMessageTask = messageBatch.Where(message => !message.IsAcknowledged).Select(message => message.NotAcknowledge());

                    await unacknowledgeMessageTask.ExecuteAndWaitForCompletion();

                    if (_protoActorPoolDispatchQueueConfiguration.CrashAppOnError)
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

        public bool CanEnqueue()
        {
            return _queueBuffer.CanPush;
        }

        public void Dispose()
        {
            _cleanUp.Dispose();
            _queueBuffer.Dispose();
            _thread?.Join();
        }
    }
}
