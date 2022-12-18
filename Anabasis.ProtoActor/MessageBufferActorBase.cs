using Anabasis.Common.Contracts;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Mailbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{

    public abstract class MessageBufferActorBase<TMessage> : IActor where TMessage : class
    {
        private readonly ILogger<MessageBufferActorBase<TMessage>>? _logger;
        private readonly MessageBufferActorConfiguration _messageBufferActorConfiguration;
        private readonly IBufferingStrategy[] _bufferingStrategies;
        private readonly List<TMessage> _messageBuffer;
        private bool _shouldGracefulyStop;

        protected MessageBufferActorBase(MessageBufferActorConfiguration messageBufferActorConfiguration, ILoggerFactory? loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger<MessageBufferActorBase<TMessage>>();
            _messageBufferActorConfiguration = messageBufferActorConfiguration;
            _bufferingStrategies = messageBufferActorConfiguration.BufferingStrategies;
            _shouldGracefulyStop = false;
            _messageBuffer = new List<TMessage>();
        }

        private bool ShouldConsumeBuffer(object message, IContext context)
        {
            if (_shouldGracefulyStop) return true;

            var shouldConsumeBuffer = false;

            foreach (var bufferingStrategy in _bufferingStrategies)
            {
                if (bufferingStrategy.ShouldConsumeBuffer(message, _messageBuffer.ToArray(), context))
                {
                    shouldConsumeBuffer = true;
                    break;
                }
            }

            return shouldConsumeBuffer;

        }

        protected virtual Task OnBufferConsumed(TMessage[] buffer)
        {
            return Task.CompletedTask;
        }

        private async Task ConsumeBuffer(IContext context)
        {
            foreach (var bufferingStrategy in _bufferingStrategies)
            {
                bufferingStrategy.Reset();
            }

            await ReceiveAsync(_messageBuffer.ToArray(), context);
        }

        public abstract Task ReceiveAsync(TMessage[] messages, IContext context);

        public async Task ReceiveAsync(IContext context)
        {
            var message = context.Message;

            if (null == message)
            {
                return;
            }

            switch (message)
            {
                case SystemMessage:
                    _logger?.LogInformation($"Received SystemMessage => {message.GetType()}");
                    break;
                case IGracefullyStopBufferActorMessage:
                    _logger?.LogInformation($"Received GracefullyStopBufferActorMessage => {message.GetType()}");
                    _shouldGracefulyStop = true;
                    break;
                case IBufferedMessageGroup:
                    break;
                default:
                    throw new InvalidOperationException($"Message {message.GetType()} is not of type {typeof(TMessage)}");
                case IBufferTimeoutDelayMessage:
                case TMessage:

                    _logger?.LogInformation($"Received message => {message.GetType()}");

                    var isBufferTimeoutDelayMessage = message is IBufferTimeoutDelayMessage;

                    if (!isBufferTimeoutDelayMessage)
                    {
                        if (message is not TMessage tMessage)
                        {
                            throw new InvalidOperationException($"Message {message.GetType()} is not of type {typeof(TMessage)}");
                        }

                        _messageBuffer.Add(tMessage);
                    }

                    var shouldConsumeBuffer = ShouldConsumeBuffer(message, context);

                    if (shouldConsumeBuffer)
                    {
                        if (_messageBuffer.Any())
                        {
                            await ConsumeBuffer(context);

                            await OnBufferConsumed(_messageBuffer.ToArray());

                            _messageBuffer.Clear();
                        }
                    }
                    else
                    {

                        Scheduler.Default.Schedule(_messageBufferActorConfiguration.ReminderSchedulingDelay, () =>
                        {
                            context.Send(context.Self, new BufferTimeoutDelayMessage());
                        });

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
