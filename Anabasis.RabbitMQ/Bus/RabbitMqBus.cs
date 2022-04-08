using Anabasis.Common;
using Anabasis.RabbitMQ.Connection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqBus : IRabbitMqBus
    {

        private readonly Dictionary<string, IRabbitMqSubscription> _existingSubscriptions;
        private readonly ISerializer _serializer;
        private readonly ILogger _logger;
        private readonly TimeSpan _defaultPublishConfirmTimeout;
        private readonly List<string> _ensureExchangeCreated;
        private readonly RabbitMqConnectionOptions _rabbitMqConnectionOptions;
        private readonly IKillSwitch _killSwitch;

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

        public void Emit(IRabbitMqEvent @event, string exchange, TimeSpan? initialVisibilityDelay = default)
        {
            Emit(new[] { @event }, exchange, initialVisibilityDelay);
        }
        public void Emit(IEnumerable<IRabbitMqEvent> events, string exchange, TimeSpan? initialVisibilityDelay = default)
        {
            //todo: handle batch emit
            foreach (var @event in events)
            {
                EnsureCreateExchange(exchange);

                var body = _serializer.SerializeObject(@event);
                var routingKey = @event.Subject;

                var basicProperties = RabbitMqConnection.GetBasicProperties();

                basicProperties.ContentType = _serializer.ContentMIMEType;
                basicProperties.ContentEncoding = _serializer.ContentEncoding;

                basicProperties.CorrelationId = $"{@event.CorrelationId}";
                basicProperties.MessageId = $"{@event.EventId}";
                basicProperties.Type = @event.GetReadableNameFromType();

                RabbitMqConnection.DoWithChannel(channel =>
                {
                    try
                    {

                        if (initialVisibilityDelay.HasValue && initialVisibilityDelay.Value > TimeSpan.Zero)
                        {
                            var delayInMilliseconds = Math.Max(1, (int)initialVisibilityDelay.Value.TotalMilliseconds);

                            basicProperties.Headers = new Dictionary<string, object>()
                            {
                                { "x-delay", delayInMilliseconds }
                            };
                        }

                        channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: basicProperties, body: body);

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

        private void EnsureCreateExchange(string exchange)
        {
            if (_ensureExchangeCreated.Contains(exchange)) return;

            RabbitMqConnection.DoWithChannel(channel =>
            {

                channel.ExchangeDeclare(exchange, type: "x-delayed-message", durable: true, autoDelete: false, new Dictionary<string, object>()
                    {
                        {"x-delayed-type", "topic"}
                    });

                var deadletterExchangeForThisSubscription = $"{exchange}-deadletters";

                channel.ExchangeDeclare(deadletterExchangeForThisSubscription, "fanout", durable: true, autoDelete: false);

                _ensureExchangeCreated.Add(exchange);

            });
        }

        public void Subscribe<TEvent>(IRabbitMqEventSubscription<TEvent> subscription)
            where TEvent : class, IRabbitMqEvent
        {
            var doesSubscriptionExist = _existingSubscriptions.ContainsKey(subscription.SubscriptionId);

            if (!doesSubscriptionExist)
            {
                RabbitMqConnection.DoWithChannel(channel =>
                {

                    EnsureCreateExchange(subscription.Exchange);

                    var queueName = channel.QueueDeclare(exclusive: true, autoDelete: true, arguments: new Dictionary<string, object>
                    {
                        {"x-dead-letter-exchange", $"{subscription.Exchange}-deadletters"},

                    }).QueueName;

                    var consumer = new AsyncEventingBasicConsumer(channel);

                    channel.QueueBind(queue: queueName,
                                      exchange: subscription.Exchange,
                                      routingKey: subscription.RoutingKey);

                    channel.BasicConsume(queue: queueName,
                                         autoAck: subscription.IsAutoAck,
                                         consumer: consumer);

                    var rabbitMqSubscription = new RabbitMqSubscription(subscription.Exchange, subscription.RoutingKey, queueName, consumer);

                    consumer.Received += async (model, basicDeliveryEventArg) =>
                    {
                        var rabbitMqQueueMessage = DeserializeRabbitMqQueueMessage(
                            basicDeliveryEventArg.BasicProperties,
                            basicDeliveryEventArg.Body.ToArray(),
                            basicDeliveryEventArg.Redelivered,
                            basicDeliveryEventArg.DeliveryTag,
                            subscription.IsAutoAck);


                        foreach (var subscriber in rabbitMqSubscription.Subscriptions)
                        {
                            try
                            {
                                await subscriber.Handle(rabbitMqQueueMessage);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"An error occured during the delivering of a message - BasicDeliveryEventArg => {basicDeliveryEventArg.ToJson()}");

                                if (_rabbitMqConnectionOptions.DoAppCrashOnFailure)
                                {
                                    _killSwitch.KillMe(ex);
                                }
                            }
                        }
                    };

                    _existingSubscriptions[rabbitMqSubscription.SubscriptionId] = rabbitMqSubscription;

                });
            }

            var rabbitMqSubscription = _existingSubscriptions[subscription.SubscriptionId];

            rabbitMqSubscription.Subscriptions.Add(subscription);

        }

        public void Unsubscribe<TEvent>(IRabbitMqEventSubscription<TEvent> subscription)
           where TEvent : class, IRabbitMqEvent
        {

            if (!_existingSubscriptions.ContainsKey(subscription.SubscriptionId))
                return;

            var subscriberDescriptor = _existingSubscriptions[subscription.SubscriptionId];

            subscriberDescriptor.Subscriptions.Remove(subscription);
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
