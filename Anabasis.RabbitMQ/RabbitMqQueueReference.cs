using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ
{
    class RabbitMqQueueReference : RabbitMqQueueReference<object>, IQueueReference, IPartitionedQueueReference
    {
        public RabbitMqQueueReference(string name, RabbitMqQueueProvider provider, RabbitMqConnectionHolder holder, IMessageSerializer serializer)
            : base(name, provider, holder, serializer)
        {
        }

        public Task PushAsync(string partitionKey, params object[] messageContents) => QueueReferenceBase.DefaultPushAsync(this, partitionKey, messageContents);
        public Task PushAsync(string partitionKey, IEnumerable<object> messageContents, TimeSpan? initialVisibilityDelay = null) => QueueReferenceBase.DefaultPushAsync(this, partitionKey, messageContents, initialVisibilityDelay);
    }

    class RabbitMqQueueReference<T> : QueueReferenceBase<T>
    {
        private readonly RabbitMqQueueProvider _provider;
        private readonly RabbitMqConnectionHolder _holder;
        private readonly IMessageSerializer _serializer;
        private readonly ITracer _tracer;

        public RabbitMqQueueReference(string name, RabbitMqQueueProvider provider, RabbitMqConnectionHolder holder, IMessageSerializer serializer)
            : base(name, provider)
        {
            _provider = provider;
            _holder = holder;
            _serializer = serializer;
            _tracer = provider._appContext.Tracer;
        }

        public override IEnumerable<IQueueMessage<T>> Pull(int? chunkSize = default(int?))
        {
            return _holder.DoWithChannel((channel) =>
            {
                var list = new List<RabbitMqQueueMessage<T>>();

                chunkSize = chunkSize ?? int.MaxValue;
                var count = 0;
                while (count < chunkSize)
                {
                    BasicGetResult result;
                    result = channel.BasicGet(Name, autoAck: false);
                    if (result == null)
                        break;
                    count++;

                    var content = GetObjectContent(result.BasicProperties, result.Body);

                    list.Add(new RabbitMqQueueMessage<T>(_holder, (T)content, result.Redelivered, result.DeliveryTag, null));
                }

                return list;
            });
        }

        public override IDisposable Subscribe(IObserver<IQueueMessage<T>> observer)
        {
            return _holder.DoWithChannel((channel) =>
            {
                // SYNC VERSION
                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (object sender, BasicDeliverEventArgs e) =>
                {
                    var parentSpanCtx = _tracer.ExtractContext(e.BasicProperties);

                    try
                    {
                        var content = GetObjectContent(e.BasicProperties, e.Body);
                        observer.OnNext(new RabbitMqQueueMessage<T>(_holder, (T)content, e.Redelivered, e.DeliveryTag, parentSpanCtx));
                    }
                    catch (Exception ex)
                    {
                        var ctx = _holder._appContext;
                        if (ctx != null)
                        {
                            ctx.Logger.LogException(ex);

                            if (parentSpanCtx != null)
                            {
                                using (var scope = ctx.Tracer.StartMessageSpan("Received unknown event", "", parentSpanCtx))
                                {
                                    scope.Span.LogError(ex);
                                }
                            }
                        }
                        throw;
                    }
                };

                var consumerTag = channel.BasicConsume(Name, autoAck: false, consumer: consumer);

                return new GenericDisposable(() => { channel.BasicCancel(consumerTag); });
            });
        }

        private object GetObjectContent(IBasicProperties props, byte[] body)
        {
            return string.IsNullOrWhiteSpace(props.Type)
                ? _serializer.DeserializeTo<T>(body)
                : _serializer.DeserializeTo(props.Type, body);
        }

        public override void ClearQueue()
        {
            _holder.DoWithChannel(channel =>
            {
                var d = _provider._delayInfrastructure;
                var maxLevel = d._maxLevel;

                for (var level = maxLevel; level >= 0; level--)
                {
                    var currentLevel = d.LevelName(Name, level);
                    channel.QueuePurge(currentLevel);
                }
                channel.QueuePurge(Name);
            });
        }
    }
}
