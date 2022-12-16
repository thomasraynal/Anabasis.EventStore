using Anabasis.Common;
using Anabasis.Common.Worker;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reactive.Disposables;

namespace Anabasis.ProtoActor
{
    public class ProtoActorPoolDispatchQueue : IProtoActorPoolDispatchQueue
    {

        private readonly IProtoActorPoolDispatchQueueConfiguration _protoActorPoolDispatchQueueConfiguration;
        private readonly ILoggerFactory? _loggerFactory;
        private readonly CompositeDisposable _cleanUp;
        private readonly Thread _thread;
        private readonly IQueueBuffer _queueBuffer;
        private readonly Action<IMessage[]> _messageHandler;
        private readonly CancellationToken _cancellationToken;
        private readonly IKillSwitch _killSwitch;

        public ProtoActorPoolDispatchQueue(string owner, 
            IProtoActorPoolDispatchQueueConfiguration protoActorPoolDispatchQueueConfiguration,
            CancellationToken cancellationToken,
            Action<IMessage[]> messageHandler,
            IQueueBuffer? queueBuffer = null,
            ILoggerFactory? loggerFactory = null,
            IKillSwitch? killSwitch = null)
        {
            Owner = owner;
            Id = $"{nameof(ProtoActorPoolDispatchQueue)}_{Guid.NewGuid()}";
            Logger = loggerFactory?.CreateLogger(GetType());

            _queueBuffer = queueBuffer ?? new SimpleQueueBuffer(protoActorPoolDispatchQueueConfiguration.MessageBufferMaxSize, 0, 0);
            _messageHandler = messageHandler;

            _cancellationToken = cancellationToken;

            _killSwitch = killSwitch ?? new KillSwitch();

            _protoActorPoolDispatchQueueConfiguration = protoActorPoolDispatchQueueConfiguration;
            _loggerFactory = loggerFactory;

            _cleanUp = new CompositeDisposable();

            _thread = new Thread(HandleWork)
            {
                IsBackground = true,
                Name = Id,
            };

            _thread.Start();

            Logger?.LogDebug($"{Id} started");
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
           return _queueBuffer.TryPush(messages, out unProcessedMessages);
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

                    if (_queueBuffer.CanPull())
                    {
                        messageBatch = _queueBuffer.Pull();

                        PulledMessagesCount += messageBatch.Length;

                        Debug.WriteLine($"Pulled batch of {messageBatch.Length} messages");

                        _messageHandler(messageBatch);

                        ProcessedMessagesCount += messageBatch.Length;

                    }

                    await Task.Delay(100);

                }
                catch (Exception exception)
                {
                    exception.Data["messages"] = messageBatch.ToJson();

                    Logger?.LogError(exception, $"An exception occured during the message consumption process");

                    this.LastError = exception;

                    var unacknowledgeMessageTask = messageBatch.Where(message => !message.IsAcknowledged).Select(message => message.NotAcknowledge());

                    await Task.WhenAll(unacknowledgeMessageTask);

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
            _queueBuffer.Dispose();
            _thread?.Join();
        }
    }
}
