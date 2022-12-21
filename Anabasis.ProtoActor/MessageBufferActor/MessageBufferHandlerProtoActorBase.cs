using Anabasis.Common;
using Microsoft.Extensions.Logging;
using Proto;
using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public abstract class MessageBufferHandlerProtoActorBase<TMessageBufferActorConfiguration> : MessageBufferProtoActorBase<TMessageBufferActorConfiguration>
           where TMessageBufferActorConfiguration : IMessageBufferActorConfiguration
    {
        private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
        public Exception? LastError { get; private set; }

        protected MessageBufferHandlerProtoActorBase(TMessageBufferActorConfiguration messageBufferActorConfiguration, ILoggerFactory? loggerFactory = null) : base(messageBufferActorConfiguration, loggerFactory)
        {
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();
        }
            
        protected virtual Task OnError(IEvent[] source, Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();

            return Task.CompletedTask;
        }

        protected override async Task ReceiveAsync(IMessage[] messages, IContext context)
        {
            Logger?.LogDebug($"{Id} => Receiving event batch : {messages.Length} event(s)");

            var events = messages.Select(message => message.Content).ToArray();

            try
            {

                var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), events.GetType());

                if (null != candidateHandler)
                {
                    await (Task)candidateHandler.Invoke(this, new object[] { events });
                }

                if (!MessageBufferActorConfiguration.SwallowUnkwownEvents)
                {
                    throw new InvalidOperationException($"{Id} cannot handle batch of type {events.GetType()}");
                }

            }
            catch (Exception exception)
            {
                LastError = exception;

                await OnError(events, exception);
            }
        }
    }
}
