using Anabasis.Api;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{

    public class RabbitMqQueue : IDisposable
    {

        public const string XDelay = "x-delay";

        private readonly ISerializer _serializer;
        private readonly RabbitMqConnection _rabbitMqConnection;
        private readonly TimeSpan _defaultPublishConfirmTimeout;
        private readonly ILogger<RabbitMqQueue> _logger;

        public RabbitMqQueue(
            string queueName,
            RabbitMqConnectionOptions settings,
            AnabasisAppContext appContext,
            ISerializer serializer,
            ILoggerFactory loggerFactory,
            RetryPolicy retryPolicy = null)
        {

            Name = queueName;

            _serializer = serializer;
            _defaultPublishConfirmTimeout =TimeSpan.FromSeconds(10);
            _logger = loggerFactory.CreateLogger<RabbitMqQueue>();
            _rabbitMqConnection = new RabbitMqConnection(settings, appContext, loggerFactory, retryPolicy);

        }

        public string Name { get; }

        public void Push<T>(string queueName, IEnumerable<T> messages, TimeSpan? initialVisibilityDelay = default)
            where T : class, IEvent
        {
            foreach (var message in messages)
            {
                var body = _serializer.SerializeObject(message);

                var basicProperties = _rabbitMqConnection.GetBasicProperties();

                basicProperties.CorrelationId = $"{message.CorrelationID}";
                basicProperties.MessageId = $"{message.EventID}";
                basicProperties.Type = message.GetTypeReadableName();

                _rabbitMqConnection.DoWithChannel(channel =>
                {
                    if (initialVisibilityDelay.HasValue && initialVisibilityDelay.Value > TimeSpan.Zero)
                    {
                        var delayInMilliseconds = Math.Max(1, (int)initialVisibilityDelay.Value.TotalSeconds);
                        basicProperties.Headers.Add(XDelay, delayInMilliseconds);
                    }
                    else
                    {
                        channel.BasicPublish(exchange: queueName, routingKey: queueName, basicProperties: basicProperties, body: body, mandatory: true);
                    }

                    channel.WaitForConfirmsOrDie(_defaultPublishConfirmTimeout);
                });
            }
        }

        public IEnumerable<RabbitMqQueueMessage> Pull(int? chunkSize = default)
        {
            return _rabbitMqConnection.DoWithChannel((channel) =>
            {

                var list = new List<RabbitMqQueueMessage>();

                chunkSize ??= int.MaxValue;

                _logger.LogDebug($"{nameof(RabbitMqQueue)}-{Name} {nameof(Pull)}({chunkSize.Value})");

                var count = 0;

                while (count < chunkSize)
                {
                    BasicGetResult result;
                    result = channel.BasicGet(Name, autoAck: false);

                    if (result == null)
                        break;

                    count++;

                    var content = (IEvent)GetObjectContent(result.BasicProperties, result.Body.ToArray());

                    list.Add(new RabbitMqQueueMessage(_rabbitMqConnection, content, result.Redelivered, result.DeliveryTag));
                }

                return list;
            });
        }

        public IDisposable Subscribe(IObserver<RabbitMqQueueMessage> observer)
        {
            return _rabbitMqConnection.DoWithChannel((channel) =>
            {

                _logger.LogDebug($"{nameof(RabbitMqQueue)}-{Name} {nameof(Subscribe)}");

                var consumer = new EventingBasicConsumer(channel);

                consumer.Received += (object sender, BasicDeliverEventArgs e) =>
                {
                    var content = (IEvent)GetObjectContent(e.BasicProperties, e.Body.ToArray());

                    _logger.LogDebug($"{nameof(RabbitMqQueue)}-{Name} Received eventId => {content.EventID}");

                    observer.OnNext(new RabbitMqQueueMessage(_rabbitMqConnection, content, e.Redelivered, e.DeliveryTag));
                };

                var consumerTag = channel.BasicConsume(Name, autoAck: false, consumer: consumer);

                return Disposable.Create(() => channel.BasicCancel(consumerTag));
            });
        }

        private object GetObjectContent(IBasicProperties props, byte[] body)
        {
            return _serializer.DeserializeObject(body, Type.GetType(props.Type));
        }

        public void ClearQueue()
        {
            _rabbitMqConnection.DoWithChannel(channel =>
            {
                channel.QueuePurge(Name);
            });
        }

        public void Dispose()
        {
            _rabbitMqConnection?.Dispose();
        }
    }
}
