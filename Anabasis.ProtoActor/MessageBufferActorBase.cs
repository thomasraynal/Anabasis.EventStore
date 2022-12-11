using Microsoft.Extensions.Logging;
using Proto;
using Proto.Mailbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public abstract class MessageBufferActorBase : IActor
    {
        private readonly ILogger<MessageBufferActorBase> _logger;
        private readonly IBufferingStrategy[] _bufferingStrategies;
        private readonly List<object> _messageBuffer;
        private bool _shouldGracefulyStop;

        protected MessageBufferActorBase(IBufferingStrategy[] bufferingStrategies, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MessageBufferActorBase>();
            _bufferingStrategies = bufferingStrategies;
            _shouldGracefulyStop = false;
            _messageBuffer = new List<object>();
        }

        private bool ShouldConsumeBuffer(object message, IContext context)
        {
            if (_shouldGracefulyStop) return true;

            var shouldConsumeBuffer = false;

            foreach (var bufferingStrategy in _bufferingStrategies)
            {
                if (bufferingStrategy.ShouldConsumeBuffer(message, context))
                {
                    shouldConsumeBuffer = true;
                    break;
                }
            }

            return shouldConsumeBuffer;

        }

        private async Task ConsumeBuffer(IContext context)
        {
            foreach (var bufferingStrategy in _bufferingStrategies)
            {
                bufferingStrategy.Reset();
            }

            await ReceiveAsync(_messageBuffer.ToArray(), context);
        }

        public abstract Task ReceiveAsync(object[] messages, IContext context);

        public async Task ReceiveAsync(IContext context)
        {
            var message = context.Message;

            switch (message)
            {
                case SystemMessage:
                    _logger.LogInformation($"Received SystemMessage => {message.GetType()}");
                    break;
                case GracefullyStopBufferActorMessage:
                    _logger.LogInformation($"Received GracefullyStopBufferActorMessage => {message.GetType()}");
                    _shouldGracefulyStop = true;
                    break;
                case IBufferedMessageGroup:
                    break;
                case IBufferTimeoutDelayMessage:
                default:

                    _logger.LogInformation($"Received message => {message.GetType()}");

                    if (message is not IBufferTimeoutDelayMessage || _shouldGracefulyStop)
                    {
                        _messageBuffer.Add(message);
                    }

                    var shouldConsumeBuffer = ShouldConsumeBuffer(message, context);

                    if (shouldConsumeBuffer)
                    {
                        await ConsumeBuffer(context);

                        _messageBuffer.Clear();
                    }
                    else
                    {
                        if (message is not IBufferTimeoutDelayMessage)
                        {
                            _messageBuffer.Add(message);
                        }
                    }

                    if (_shouldGracefulyStop)
                    {
                        context.Stop(context.Self);
                    }

                    break;
            }

        }
    }
}
