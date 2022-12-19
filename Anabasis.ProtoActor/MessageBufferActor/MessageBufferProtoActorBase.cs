using Anabasis.Common;
using Anabasis.Common.Contracts;
using Anabasis.ProtoActor.System;
using Microsoft.Extensions.Logging;
using Proto;
using Proto.Mailbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{

    public abstract class MessageBufferProtoActorBase : IActor, IDisposable
    {

        protected readonly IMessageBufferActorConfiguration _messageBufferActorConfiguration;

        private readonly IBufferingStrategy[] _bufferingStrategies;
        private readonly List<IMessage> _messageBuffer;
        private readonly CompositeDisposable _cleanUp;

        public string Id { get; }
        public ILogger<MessageBufferProtoActorBase>? Logger { get; }

        protected MessageBufferProtoActorBase(IMessageBufferActorConfiguration messageBufferActorConfiguration, ILoggerFactory? loggerFactory = null)
        {
            _messageBufferActorConfiguration = messageBufferActorConfiguration;
            _bufferingStrategies = messageBufferActorConfiguration.BufferingStrategies;
            _messageBuffer = new List<IMessage>();
            _cleanUp = new CompositeDisposable();

            Id = this.GetUniqueIdFromType();
            Logger = loggerFactory?.CreateLogger<MessageBufferProtoActorBase>();
        }

        private bool ShouldConsumeBuffer(object message, IContext context)
        {

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

        protected virtual Task OnReceivedGracefullyStop(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnReceivedIdleTimout(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnBufferConsumed(IMessage[] buffer)
        {
            return Task.CompletedTask;
        }

        private async Task ConsumeBufferAndAckMessages(IContext context)
        {
            foreach (var bufferingStrategy in _bufferingStrategies)
            {
                bufferingStrategy.Reset();
            }

            await ReceiveAsync(_messageBuffer.ToArray(), context);

            await _messageBuffer.Select(message => message.Acknowledge()).ExecuteAndWaitForCompletion();
        }

        public void AddToCleanup(IDisposable disposable)
        {
            _cleanUp.Add(disposable);
        }

        protected abstract Task ReceiveAsync(IMessage[] messages, IContext context);

        public async Task ReceiveAsync(IContext context)
        {
            var message = context.Message;

            if (null == message)
            {
                return;
            }

            switch (message)
            {
                case Started:

                    if (message is Started)
                    {
                        context.SetReceiveTimeout(_messageBufferActorConfiguration.IdleTimeoutFrequency);
                    }

                    if (message is ReceiveTimeout)
                    {
                        await OnReceivedIdleTimout(context);
                    }

                    Logger?.LogInformation($"Received SystemMessage => {message.GetType()}");

                    break;
                case IGracefullyStopBufferActorMessage:

                    await OnReceivedGracefullyStop(context);

                    await ConsumeBufferAndAckMessages(context);

                    await OnBufferConsumed(_messageBuffer.ToArray());

                    _messageBuffer.Clear();

                    context.Stop(context.Self);

                    break;
                default:
                    throw new InvalidOperationException($"Message {message.GetType()} is not of type {typeof(IMessage)}");
                case IBufferTimeoutDelayMessage:
                case IMessage:

                    Logger?.LogInformation($"Received message => {message.GetType()}");

                    var isBufferTimeoutDelayMessage = message is IBufferTimeoutDelayMessage;

                    if (!isBufferTimeoutDelayMessage)
                    {
                        if (message is not IMessage tMessage)
                        {
                            throw new InvalidOperationException($"Message {message.GetType()} is not of type {typeof(IMessage)}");
                        }

                        _messageBuffer.Add(tMessage);
                    }

                    var shouldConsumeBuffer = ShouldConsumeBuffer(message, context);

                    if (shouldConsumeBuffer)
                    {
                        if (_messageBuffer.Any())
                        {
                            await ConsumeBufferAndAckMessages(context);

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

                    break;

            }

        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}
