using Anabasis.Common;
using Anabasis.ProtoActor.MessageBufferActor;
using Anabasis.ProtoActor.System;
using Microsoft.Extensions.Logging;
using Proto;
using System;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public abstract class MessageHandlerProtoActorBase : IActor, IDisposable
    {

        private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private readonly IMessageHandlerActorConfiguration _messageHandlerActorConfiguration;
        private readonly CompositeDisposable _cleanUp;

        public ILogger<MessageHandlerProtoActorBase>? Logger { get; }
        public string Id { get; }
        public Exception? LastError { get; private set; }

        protected MessageHandlerProtoActorBase(IMessageHandlerActorConfiguration messageHandlerActorConfiguration, ILoggerFactory? loggerFactory = null)
        {
            Logger = loggerFactory?.CreateLogger<MessageHandlerProtoActorBase>();
            Id = this.GetUniqueIdFromType();

            _cleanUp = new CompositeDisposable();
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
            _messageHandlerActorConfiguration = messageHandlerActorConfiguration;
        }

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
                        context.SetReceiveTimeout(_messageHandlerActorConfiguration.IdleTimeoutFrequency);
                    }

                    if (message is ReceiveTimeout)
                    {
                        await OnReceivedIdleTimout(context);
                    }

                    Logger?.LogInformation($"Received SystemMessage => {message.GetType()}");

                    break;
                case IGracefullyStopBufferActorMessage:
                  
                    await OnReceivedGracefullyStop(context);

                    context.Stop(context.Self);

                    break;
                default:
                    throw new InvalidOperationException($"Message {message.GetType()} is not of type {typeof(IMessage)}");
                case IBufferTimeoutDelayMessage:
                case IMessage:

                    Logger?.LogInformation($"Received message => {message.GetType()}");

                    if (message is not IMessage tMessage)
                    {
                        throw new InvalidOperationException($"Message {message.GetType()} is not of type {typeof(IMessage)}");
                    }

                    await ConsumeEvent(tMessage.Content);

                    await tMessage.Acknowledge();

                    break;

            }
        }

        protected virtual Task OnReceivedGracefullyStop(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnReceivedIdleTimout(IContext context)
        {
            return Task.CompletedTask;
        }

        protected virtual Task OnError(IEvent source, Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();

            return Task.CompletedTask;
        }

        private async Task ConsumeEvent(IEvent @event)
        {
            try
            {

                Logger?.LogDebug($"{Id} => Receiving event {@event.EntityId} - {@event.GetType()}");

                var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), @event.GetType());

                if (null != candidateHandler)
                {
                    await (Task)candidateHandler.Invoke(this, new object[] { @event });
                }

                if (!_messageHandlerActorConfiguration.SwallowUnkwownEvents)
                {
                    throw new InvalidOperationException($"{Id} cannot handle event {@event.GetType()}");
                }

            }
            catch (Exception exception)
            {
                LastError = exception;

                await OnError(@event, exception);
            }
        }

        public void AddToCleanup(IDisposable disposable)
        {
            _cleanUp.Add(disposable);
        }
        public override bool Equals(object? obj)
        {
            return obj is MessageHandlerProtoActorBase actor && Id == actor.Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }
    }
}
