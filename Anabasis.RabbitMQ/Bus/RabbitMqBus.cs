using Anabasis.Common;
using Anabasis.RabbitMQ.Connection;
using Anabasis.RabbitMQ.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    //todo: handle faulty message (dequeue count > n)

    //USE CASE EXCHANGE=>
    //todo: temp/permanent
    //todo: autocreate?

    //USE CASE QUEUE=>
    //todo: temp/permanent binding to exchange
    //todo: one exchange and one queue 

    public class RabbitMqBus : IRabbitMqBus
    {

        private readonly Dictionary<string, IRabbitMqSubscription> _existingSubscriptions;
        private readonly ISerializer _serializer;
        private readonly ILogger _logger;
        private readonly TimeSpan _defaultPublishConfirmTimeout;
        private readonly List<string> _ensureExchangeCreated;
        private readonly RabbitMqConnectionOptions _rabbitMqConnectionOptions;
        private readonly IKillSwitch _killSwitch;
        private readonly AnabasisAppContext _appContext;

        public bool IsInitialized { get; private set; }
        public string BusId { get; }
        public IRabbitMqConnection RabbitMqConnection { get; }
        public IConnectionStatusMonitor ConnectionStatusMonitor { get; }

        public RabbitMqBus(RabbitMqConnectionOptions rabbitMqConnectionOptions,
                   AnabasisAppContext appContext,
                   ILoggerFactory loggerFactory,
                   ISerializer serializer,
                   IKillSwitch? killSwitch = null,
                   RetryPolicy? retryPolicy = null)
        {
            BusId = $"{nameof(RabbitMqBus)}_{Guid.NewGuid()}";

            _logger = loggerFactory.CreateLogger<RabbitMqBus>();
            _serializer = serializer;
            _defaultPublishConfirmTimeout = TimeSpan.FromSeconds(10);
            _existingSubscriptions = new Dictionary<string, IRabbitMqSubscription>();
            _ensureExchangeCreated = new List<string>();
            _rabbitMqConnectionOptions = rabbitMqConnectionOptions;
            _killSwitch = killSwitch ?? new KillSwitch();
            _appContext = appContext;

            RabbitMqConnection = new RabbitMqConnection(rabbitMqConnectionOptions, appContext, loggerFactory, retryPolicy);
            ConnectionStatusMonitor = new RabbitMqConnectionStatusMonitor(RabbitMqConnection, loggerFactory);

        }

        public async Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            if (ConnectionStatusMonitor.IsConnected) return;

            var waitUntilMax = DateTime.UtcNow.Add(null == timeout ? Timeout.InfiniteTimeSpan : timeout.Value);

            while (!ConnectionStatusMonitor.IsConnected && DateTime.UtcNow > waitUntilMax)
            {
                await Task.Delay(100);
            }

            if (!ConnectionStatusMonitor.IsConnected) throw new InvalidOperationException("Unable to connect");
        }

        public void Emit(IRabbitMqEvent @event,
            string exchange,
            string exchangeType = "topic",
            TimeSpan? initialVisibilityDelay = default,
            TimeSpan? expiration = default,
            bool isPersistent = true,
            bool isMandatory = false,
            (string headerKey, string headerValue)[]? additionalHeaders = null)
        {
            Emit(new[] { @event }, exchange, exchangeType, initialVisibilityDelay, expiration, isPersistent, isMandatory, additionalHeaders);
        }

        public void Emit(IEnumerable<IRabbitMqEvent> events,
            string exchange,
            string exchangeType = "topic",
            TimeSpan? initialVisibilityDelay = default,
            TimeSpan? expiration = default,
            bool isPersistent = true,
            bool isMandatory = false,
            (string headerKey, string headerValue)[]? additionalHeaders = null)
        {
            CreateExchangeIfNotExist(exchange, exchangeType);

            foreach (var @event in events)
            {

                var body = _serializer.SerializeObject(@event);
                var routingKey = @event.Subject;

                var basicProperties = RabbitMqConnection.GetBasicProperties();

                if (expiration.HasValue && expiration.Value > TimeSpan.Zero)
                {
                    var expirationInMilliseconds = Math.Max(1, (int)expiration.Value.TotalMilliseconds);

                    basicProperties.Expiration = $"{expirationInMilliseconds}";
                }

                basicProperties.Persistent = isPersistent;
                basicProperties.ContentType = _serializer.ContentMIMEType;
                basicProperties.ContentEncoding = _serializer.ContentEncoding;
                basicProperties.CorrelationId = $"{@event.CorrelationId}";
                basicProperties.MessageId = $"{@event.EventId}";
                basicProperties.Type = @event.GetReadableNameFromType();
                basicProperties.AppId = _appContext.ApplicationNameAndApiVersion;
                basicProperties.UserId = _appContext.MachineName;
                basicProperties.Timestamp = @event.Timestamp.ToAmqpTimestamp();

                basicProperties.Headers = new Dictionary<string, object>();

                if (null != @event.CauseId)
                {
                    basicProperties.Headers.Add("causeId", @event.CauseId);
                }

                if (null != additionalHeaders)
                {
                    foreach (var (headerKey, headerValue) in additionalHeaders)
                    {
                        basicProperties.Headers.Add(headerKey, headerValue);
                    }
                }

                RabbitMqConnection.DoWithChannel(channel =>
                {
                    try
                    {

                        if (initialVisibilityDelay.HasValue && initialVisibilityDelay.Value > TimeSpan.Zero)
                        {
                            var delayInMilliseconds = Math.Max(1, (int)initialVisibilityDelay.Value.TotalMilliseconds);

                            basicProperties.Headers.Add("x-delay", delayInMilliseconds);
                        }

                        channel.BasicPublish(exchange: exchange, routingKey: routingKey, mandatory: isMandatory, basicProperties: basicProperties, body: body);

                        channel.WaitForConfirmsOrDie(_defaultPublishConfirmTimeout);

                    }
                    catch (OperationInterruptedException operationInterruptedException)
                    {
                        _logger.LogError(operationInterruptedException, $"{nameof(OperationInterruptedException)} occured - ShutdownReason => {operationInterruptedException.ShutdownReason?.ToJson()}");
                    }
                });
            }
        }


        public IRabbitMqQueueMessage[] Pull(string queueName, bool isAutoAck = false, int? chunkSize = default)
        {
            return RabbitMqConnection.DoWithChannel((channel) =>
            {

                var rabbitMqQueueMessages = new List<IRabbitMqQueueMessage>();

                chunkSize ??= int.MaxValue;

                _logger.LogDebug($"{BusId} - {nameof(Pull)}({chunkSize.Value})");

                var count = 0;

                while (count < chunkSize)
                {
                    BasicGetResult result;

                    result = channel.BasicGet(queueName, autoAck: isAutoAck);

                    if (result == null)
                        break;

                    count++;

                    var rabbitMqQueueMessage = DeserializeRabbitMqQueueMessage(result.BasicProperties,
                        result.Body.ToArray(),
                        result.Redelivered,
                        result.DeliveryTag,
                        isAutoAck);

                    rabbitMqQueueMessages.Add(rabbitMqQueueMessage);
                }

                return rabbitMqQueueMessages.ToArray();
            });
        }

        private IRabbitMqQueueMessage DeserializeRabbitMqQueueMessage(IBasicProperties basicProperties, byte[] body, bool redelivered, ulong deliveryTag, bool isAutoAck)
        {
            var type = basicProperties.Type.GetTypeFromReadableName();
            var message = (IRabbitMqEvent)_serializer.DeserializeObject(body, type);

            return new RabbitMqQueueMessage(message.MessageId, RabbitMqConnection, type, message, redelivered, deliveryTag, isAutoAck);
        }

        private void CreateExchangeIfNotExist(string exchange, string exchangeType = "topic", bool durable = true, bool autodelete = false)
        {
            if (_ensureExchangeCreated.Contains(exchange)) return;

            RabbitMqConnection.DoWithChannel(channel =>
            {

                channel.ExchangeDeclare(exchange, type: "x-delayed-message", durable: durable, autoDelete: autodelete, new Dictionary<string, object>()
                    {
                        {"x-delayed-type",exchangeType}
                    });

                var deadletterExchangeForThisSubscription = $"{exchange}-deadletters";

                channel.ExchangeDeclare(deadletterExchangeForThisSubscription, "fanout", durable: durable, autoDelete: false);

                _ensureExchangeCreated.Add(exchange);

            });
        }

        public void SubscribeToExchange<TEvent>(IRabbitMqEventSubscription<TEvent> subscription)
            where TEvent : class, IRabbitMqEvent
        {
            var doesSubscriptionExist = _existingSubscriptions.ContainsKey(subscription.SubscriptionId);

            if (!doesSubscriptionExist)
            {
                RabbitMqConnection.DoWithChannel(channel =>
                {

                    if (subscription.RabbitMqExchangeConfiguration.CreateExchangeIfNotExist)
                    {
                        CreateExchangeIfNotExist(subscription.RabbitMqExchangeConfiguration.ExchangeName,
                            subscription.RabbitMqExchangeConfiguration.ExchangeType,
                            subscription.RabbitMqExchangeConfiguration.IsDurable,
                            subscription.RabbitMqExchangeConfiguration.IsAutoDelete);
                    }

                    var queueName = channel.QueueDeclare(
                        queue: subscription.RabbitMqQueueConfiguration.QueueName ?? string.Empty,
                        exclusive: subscription.RabbitMqQueueConfiguration.IsExclusive,
                        durable: subscription.RabbitMqQueueConfiguration.IsDurable,
                        autoDelete: subscription.RabbitMqQueueConfiguration.IsAutoDelete,
                        arguments: new Dictionary<string, object>
                        {
                            {"x-dead-letter-exchange", $"{subscription.RabbitMqExchangeConfiguration.ExchangeName}-deadletters"},

                        }).QueueName;

                    var consumer = new AsyncEventingBasicConsumer(channel);

                    channel.QueueBind(queue: queueName,
                                      exchange: subscription.RabbitMqExchangeConfiguration.ExchangeName,
                                      routingKey: subscription.RabbitMqQueueConfiguration.RoutingKey);

                    channel.BasicConsume(queue: queueName,
                                         autoAck: subscription.RabbitMqQueueConfiguration.IsAutoAck,
                                         consumer: consumer);

                    var rabbitMqSubscription = new RabbitMqSubscription(queueName,
                        subscription,
                        consumer);

                    consumer.Received += async (model, basicDeliveryEventArg) =>
                    {


                        var rabbitMqQueueMessage = DeserializeRabbitMqQueueMessage(
                            basicDeliveryEventArg.BasicProperties,
                            basicDeliveryEventArg.Body.ToArray(),
                            basicDeliveryEventArg.Redelivered,
                            basicDeliveryEventArg.DeliveryTag,
                            subscription.RabbitMqQueueConfiguration.IsAutoAck);

                        try
                        {
                            await rabbitMqSubscription.Subscription.Handle(rabbitMqQueueMessage);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"An error occured during the delivering of a message - BasicDeliveryEventArg => {basicDeliveryEventArg.ToJson()}");

                            if (_rabbitMqConnectionOptions.DoAppCrashOnFailure)
                            {
                                _killSwitch.KillProcess(ex);
                            }
                        }
                    };

                });
            }
        }

        public void UnsubscribeFromExchange<TEvent>(IRabbitMqEventSubscription<TEvent> subscription)
           where TEvent : class, IRabbitMqEvent
        {

            if (!_existingSubscriptions.ContainsKey(subscription.SubscriptionId))
                return;

            var subscriberDescriptor = _existingSubscriptions[subscription.SubscriptionId];

            RabbitMqConnection.DoWithChannel(channel =>
            {
                channel.BasicCancel(subscriberDescriptor.Consumer.ConsumerTags.First());
            });

        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var healthCheckDescription = $"{nameof(RabbitMqBus)} healthcheck";

            var healthCheckResult = HealthCheckResult.Healthy(healthCheckDescription);

            RabbitMqConnection.DoWithChannel(model =>
            {
                if (!model.IsOpen)
                {
                    var healthCheckMessages = new Dictionary<string, object>()
                    {
                        { "RabbitMQ channel is not open", $"HostNameHostName => {_rabbitMqConnectionOptions.HostName}" }
                    };

                    healthCheckResult = HealthCheckResult.Unhealthy(healthCheckDescription, data: healthCheckMessages);
                }
            });

            return Task.FromResult(healthCheckResult);

        }

        public void Dispose()
        {
            RabbitMqConnection.Dispose();
        }
    }
}
