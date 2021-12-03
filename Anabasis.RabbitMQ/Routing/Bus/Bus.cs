using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Routing.Bus;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;

namespace RabbitMQPlayground.Routing
{
    public class Bus : IBus
    {

        private readonly Dictionary<string, IRabbitMqSubscription> _existingSubscriptions;
        private readonly IRabbitMqConnection _rabbitMqConnection;
        private readonly IRabbitMqEventSerializer _rabbitMqEventSerializer;
        private readonly IBusConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly TimeSpan _defaultPublishConfirmTimeout;

        public const string XDelay = "x-delay";

        public string Id { get; }

        public Bus(IBusConfiguration configuration,
                   IRabbitMqConnection rabbitMqConnection,
                   ILoggerFactory loggerFactory,
                   IRabbitMqEventSerializer rabbitMqEventSerializer)
        {
            Id = $"{nameof(Bus)}_{Guid.NewGuid()}";

            _logger = loggerFactory.CreateLogger<Bus>();
            _rabbitMqEventSerializer = rabbitMqEventSerializer;
            _configuration = configuration;
            _rabbitMqConnection = rabbitMqConnection;
            _defaultPublishConfirmTimeout = TimeSpan.FromSeconds(10);

            _existingSubscriptions = new Dictionary<string, IRabbitMqSubscription>();

        }

        public void Emit(IRabbitMqEvent @event, string exchange, TimeSpan? initialVisibilityDelay = default)
        {
            Emit(new[] { @event }, exchange, initialVisibilityDelay);
        }
        public void Emit(IEnumerable<IRabbitMqEvent> events, string exchange, TimeSpan? initialVisibilityDelay = default)
        {

            foreach (var @event in events)
            {
                var body = _rabbitMqEventSerializer.Serializer.Serialize(@event);
                var routingKey = _rabbitMqEventSerializer.GetRoutingKey(@event);

                var basicProperties = _rabbitMqConnection.GetBasicProperties();

                basicProperties.ContentType = _rabbitMqEventSerializer.Serializer.ContentMIMEType;
                basicProperties.ContentEncoding = _rabbitMqEventSerializer.Serializer.ContentEncoding;

                basicProperties.CorrelationId = $"{@event.CorrelationID}";
                basicProperties.MessageId = $"{@event.EventID}";
                basicProperties.Type = @event.GetTypeReadableName();

                _rabbitMqConnection.DoWithChannel(channel =>
                {

                    if (initialVisibilityDelay.HasValue && initialVisibilityDelay.Value > TimeSpan.Zero)
                    {
                        var delayInMilliseconds = Math.Max(1, (int)initialVisibilityDelay.Value.TotalSeconds);
                        basicProperties.Headers.Add(XDelay, delayInMilliseconds);
                    }
                    else
                    {
                        channel.BasicPublish(exchange: exchange, routingKey: routingKey, basicProperties: basicProperties, body: body);
                    }

                    channel.WaitForConfirmsOrDie(_defaultPublishConfirmTimeout);

                });
            }
        }

        private IRabbitMqSubscription GetOrCreateEventSubscriberDescriptor(IRabbitMqEventSubscription subscription)
        {

            var doesSubscriptionExist = _existingSubscriptions.ContainsKey(subscription.SubscriptionId);

            if (doesSubscriptionExist)
            {

                _rabbitMqConnection.DoWithChannel(channel =>
                {

                    if (!channel.DoesExchangeExist(subscription.Exchange))
                    {
                        channel.ExchangeDeclare(exchange: subscription.Exchange, type: "topic", durable: false, autoDelete: true);
                    }

                    var deadletterExchangeForThisSubscription = $"{subscription.Exchange}-rejected";

                    if (!channel.DoesExchangeExist(deadletterExchangeForThisSubscription))
                    {
                        channel.ExchangeDeclare(deadletterExchangeForThisSubscription, "fanout", durable: false, autoDelete: true);
                    }

                    var queueName = channel.QueueDeclare(exclusive: true, autoDelete: true, arguments: new Dictionary<string, object>
                    {
                        {"x-dead-letter-exchange", $"{subscription.Exchange}-rejected"},

                    }).QueueName;

                    var consumer = new EventingBasicConsumer(channel);

                    channel.QueueBind(queue: queueName,
                                       exchange: subscription.Exchange,
                                       routingKey: subscription.RoutingKey);

                    channel.BasicConsume(queue: queueName,
                                         autoAck: false,
                                         consumer: consumer);

                    var rabbitMqSubscription = new RabbitMqSubscription(subscription.Exchange, subscription.RoutingKey, queueName, consumer);

                    consumer.Received += (model, arg) =>
                    {
                        try
                        {

                            var body = arg.Body;
                            var type = Type.GetType(arg.BasicProperties.Type);
                            var message = (IRabbitMqEvent)_rabbitMqEventSerializer.Serializer.Deserialize(body.ToArray(), type);

                            foreach (var subscriber in rabbitMqSubscription.Subscriptions)
                            {
                                //todo: use observables

                                //if there is an exception in message in the consumer, we immediatly fail and nack the message
                                //that would mean SOME subscriber may have to process twice but we want to ensure the consumer keep failing until the message is correctly processed
                                subscriber.OnEvent(message).Wait();
                            }

                            channel.BasicAck(deliveryTag: arg.DeliveryTag, multiple: false);

                        }
                        catch (Exception ex)
                        {
                            channel.BasicNack(deliveryTag: arg.DeliveryTag, multiple: true, requeue: false);
                            _logger.LogError($"Error while handling event {arg.BasicProperties.Type}", ex);
                        }

                    };

                    _existingSubscriptions[rabbitMqSubscription.SubscriptionId] = rabbitMqSubscription;

                });

            }

            var rabbitMqSubscription = _existingSubscriptions[subscription.SubscriptionId];

            rabbitMqSubscription.Subscriptions.Add(subscription);

            return _existingSubscriptions[subscription.SubscriptionId];

        }
        public IRabbitMqSubscription Subscribe(IRabbitMqEventSubscription subscription)
        {
            return GetOrCreateEventSubscriberDescriptor(subscription);
        }

        public void Unsubscribe<TEvent>(IRabbitMqEventSubscription subscription)
        {

            if (!_existingSubscriptions.ContainsKey(subscription.SubscriptionId))
                return;

            var subscriberDescriptor = _existingSubscriptions[subscription.SubscriptionId];

            subscriberDescriptor.Subscriptions.Remove(subscription);
        }

        public void Dispose()
        {
            _rabbitMqConnection.Dispose();
        }
    }
}
