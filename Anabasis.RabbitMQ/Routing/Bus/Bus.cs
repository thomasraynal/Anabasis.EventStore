using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Routing.Bus;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{
    public class Bus : IBus
    {

        private readonly Dictionary<string,RabbitMqSubscription> _existingSubscriptions;
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

            _existingSubscriptions = new Dictionary<string, RabbitMqSubscription>();

        }

        public void Emit(IEvent @event, string exchange, TimeSpan? initialVisibilityDelay = default)
        {
            Emit(new[] { @event }, exchange, initialVisibilityDelay);
        }
        public void Emit(IEnumerable<IEvent> events, string exchange, TimeSpan? initialVisibilityDelay = default)
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

        private RabbitMqSubscription GetOrCreateEventSubscriberDescriptor(IEventSubscription subscription)
        {

            var doesSubscriptionExist = _existingSubscriptions.ContainsKey(subscription.SubscriptionId);

            if (doesSubscriptionExist) {

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

                    var rabbitMqSubscription = new RabbitMqSubscription(subscription.Exchange, subscription.RoutingKey, consumer, queueName);

                    consumer.Received += (model, arg) =>
                    {
                        try
                        {

                            var body = arg.Body;
                            var type = Type.GetType(arg.BasicProperties.Type);
                            var message = (IEvent)_rabbitMqEventSerializer.Serializer.Deserialize(body.ToArray(), type);

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
        public RabbitMqSubscription Subscribe(IEventSubscription subscription)
        {
            return GetOrCreateEventSubscriberDescriptor(subscription);
        }

        public void Unsubscribe<TEvent>(IEventSubscription subscription)
        {

            if (!_existingSubscriptions.ContainsKey(subscription.SubscriptionId))
                return;

            var subscriberDescriptor = _existingSubscriptions[subscription.SubscriptionId];

            subscriberDescriptor.Subscriptions.Remove(subscription);
        }

        private void DeclareCommandsExchanges()
        {
            _channel.ExchangeDeclare(CommandsExchange, "direct", durable: false, autoDelete: true);
            _channel.ExchangeDeclare(RejectedCommandsExchange, "fanout", durable: false, autoDelete: true);
        }

        private string CreateCommandResultHandlingQueue()
        {
            var queueName = _channel.QueueDeclare(exclusive: true, durable: false, autoDelete: true).QueueName;

            var resultHandler = new EventingBasicConsumer(_channel);

            resultHandler.Received += (model, arg) =>
            {
                var body = arg.Body;
                var response = Encoding.UTF8.GetString(body);
                var correlationId = arg.BasicProperties.CorrelationId;

                if (_commandResults.ContainsKey(correlationId))
                {
                    var task = _commandResults[correlationId];
                    var type = Type.GetType(arg.BasicProperties.Type);
                    var message = (ICommandResult)_rabbitMqEventSerializer.Serializer.Deserialize(body, type);

                    task.SetResult(message);

                    _commandResults.Remove(correlationId);

                }
            };

            _channel.BasicConsume(
               consumer: resultHandler,
               queue: queueName,
               autoAck: true);

            return queueName;
        }

        public Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : ICommandResult
        {
            var task = new TaskCompletionSource<ICommandResult>();
            var properties = _channel.CreateBasicProperties();
            var correlationId = Guid.NewGuid().ToString();

            var body = _rabbitMqEventSerializer.Serializer.Serialize(command);

            properties.ContentType = _rabbitMqEventSerializer.Serializer.ContentMIMEType;
            properties.ContentEncoding = _rabbitMqEventSerializer.Serializer.ContentEncoding;
            properties.Type = command.GetType().ToString();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = _commandsResultQueue;

            _commandResults.Add(correlationId, task);

            var cancel = new CancellationTokenSource(_configuration.CommandTimeout);
            cancel.Token.Register(() => task.TrySetCanceled(), false);

            _channel.BasicPublish(
                exchange: CommandsExchange,
                routingKey: command.Target,
                basicProperties: properties,
                mandatory : true,
                body: body);

            return task.Task.ContinueWith(t =>
            {

                if (t.Result.IsError)
                {
                    throw new CommandFailureException(t.Result as ICommandErrorResult);
                }

                return (TCommandResult)t.Result;

            }, cancel.Token);

        }

        public void Handle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            var target = subscription.Target;
            var subscriptionId = subscription.SubscriptionId;

            if (_commandSubscriberDescriptors.Any(subscriber => subscriber.SubscriptionId == subscriptionId)) throw new InvalidOperationException($"Bus already have an handler for {subscriptionId}");

            if (!_commandSubscriberDescriptors.Any(subscriber => subscriber.ExchangeName == target))
            {
                _channel.QueueDeclare(queue: target, durable: false, exclusive: true, autoDelete: true, arguments: new Dictionary<string, object>
                    {
                        {"x-dead-letter-exchange", RejectedCommandsExchange},
                    });

                _channel.QueueBind(queue: target, exchange: CommandsExchange, routingKey: target);

            }

            var consumer = new EventingBasicConsumer(_channel);

            _channel.BasicConsume(queue: target,
                                  autoAck: false,
                                  consumer: consumer);

            var subscriberDescriptor = new CommandSubscriberDescriptor(consumer, target, subscriptionId, subscription);

            consumer.Received += (model, arg) =>
            {
                var properties = arg.BasicProperties;
                var body = arg.Body;

                var replyProperties = _channel.CreateBasicProperties();
                replyProperties.CorrelationId = properties.CorrelationId;
                replyProperties.ContentType = _rabbitMqEventSerializer.Serializer.ContentMIMEType;
                replyProperties.ContentEncoding = _rabbitMqEventSerializer.Serializer.ContentEncoding;

                try
                {
                    var type = Type.GetType(properties.Type);
                    var message = (ICommand)_rabbitMqEventSerializer.Serializer.Deserialize(body, type);

                    var descriptor = _commandSubscriberDescriptors.FirstOrDefault(subscriber => subscriber.SubscriptionId == $"{subscriber.ExchangeName}.{type}");

                    if (null == descriptor) throw new NotImplementedException($"No command handler for {type}");

                    //todo: allow task
                    var commandResult = descriptor.Subscription.OnCommand(message);

                    replyProperties.Type = typeof(TCommandResult).ToString();

                    var replyMessage = _rabbitMqEventSerializer.Serializer.Serialize(commandResult);

                    _channel.BasicAck(deliveryTag: arg.DeliveryTag, multiple: false);
                    _channel.BasicPublish(exchange: string.Empty, routingKey: properties.ReplyTo, mandatory: true, basicProperties: replyProperties, body: replyMessage);
                   
                }
                catch (Exception ex)
                {
                    _channel.BasicReject(deliveryTag: arg.DeliveryTag, requeue: false);

                    //todo: error handler with message and code
                    var error = new CommandErrorResult()
                    {
                        ErrorCode = 500,
                        ErrorMessage = "Unable to process the commmand"
                    };

                    replyProperties.Type = typeof(CommandErrorResult).ToString();

                    var replyMessage = _rabbitMqEventSerializer.Serializer.Serialize(error);

                    _channel.BasicPublish(exchange: string.Empty, routingKey: properties.ReplyTo, mandatory: true, basicProperties: replyProperties, body: replyMessage);

                    _logger.LogError($"Error while handling command {arg.BasicProperties.Type}", ex);

                }

            };

            _commandSubscriberDescriptors.Add(subscriberDescriptor);
        }

        public void UnHandle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
             where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            var key = subscription.Target;

            var subscriberDescriptor = _commandSubscriberDescriptors.FirstOrDefault(s => s.SubscriptionId == key);

            if (null == subscriberDescriptor) return;

            _commandSubscriberDescriptors.Remove(subscriberDescriptor);
        }

        public void Dispose()
        {
            _rabbitMqConnection.Dispose();
        }
    }
}
