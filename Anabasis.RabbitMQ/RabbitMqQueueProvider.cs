using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    /// <summary>
    /// TO IMPROVE : http://gigi.nullneuron.net/gigilabs/resilient-connections-with-rabbitmq-net-client/
    /// 
    /// Update 21st November 2015: Apparently handling reconnect as shown here causes problems with acknowledgements when the connection breaks. 
    /// The best approach is a combination of iterative reconnect for the first connection, and AutomaticRecoveryEnabled for subsequent disconnects.
    /// </summary>
    public class RabbitMqQueueProvider
    {
        private readonly RabbitMqConnectionOptions _settings;
        private readonly ISerializer _serializer;
        internal readonly RabbitMqConnectionHolder _holder;
        private readonly HttpClient _httpClient;
        private const string ONLY_ADRESS = "adress";
        internal readonly TimeSpan _defaultPublishConfirmTimeout = TimeSpan.FromSeconds(10);

        public const int DEFAULT_RABBITMQ_PREFETCH_COUNT = 100;

        public RabbitMqQueueProvider(RabbitMqConnectionOptions settings, BeezUPAppContext appContext, ISerializer serializer, ushort prefetchCount = DEFAULT_RABBITMQ_PREFETCH_COUNT, RetryPolicy retryPolicy = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _appContext = appContext ?? throw new ArgumentNullException(nameof(appContext));
            _logger = _appContext.Logger;
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _holder = new RabbitMqConnectionHolder(settings, appContext, prefetchCount, retryPolicy);

            _httpClient = new HttpClientBuilder(appContext).Build();
            var byteArray = Encoding.ASCII.GetBytes($"{_settings.User}:{_settings.Password}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
        }

        public RabbitMqQueueProvider(string hostName, string user, string password, BeezUPAppContext appContext, IMessageSerializer serializer, int port = RabbitMqConnectionOptions.RABBIT_MQ_DEFAULT_PORT, int managerPort = RabbitMqConnectionOptions.RABBIT_MQ_DEFAULT_MANAGER_PORT, ushort prefetchCount = 10, RetryPolicy retryPolicy = null)
            : this(new RabbitMqConnectionOptions { Host = hostName, User = user, Password = password, Port = port, ManagerPort = managerPort }, appContext, serializer, prefetchCount, retryPolicy)
        { }


        public override IQueueReference<T> GetQueue<T>(string queueName, QueueManagerConfigurationSectionQueue configuration = null)
        {
            return new RabbitMqQueueReference<T>(queueName, this, _holder, _serializer);
        }

        public override IQueueReference GetQueue(string queueName, QueueManagerConfigurationSectionQueue configuration = null)
        {
            return new RabbitMqQueueReference(queueName, this, _holder, _serializer);
        }

        public override IPartitionedQueueReference GetPartitionedQueue(string queueName, QueueManagerConfigurationSectionQueue configuration = null)
        {
            return new RabbitMqQueueReference(queueName, this, _holder, _serializer);
        }

        /*
        https://www.cloudamqp.com/docs/delayed-messages.html
        BETTER IMPLEMENTATION :
        https://docs.particular.net/transports/rabbitmq/delayed-delivery
        https://github.com/Particular/NServiceBus.RabbitMQ
        ===================== How it works
        When an endpoint is started, the transport declares a set of topic exchanges, queues, and bindings that work together to provide 
        the necessary infrastructure to support delayed messages. 
        Exchanges and queues are grouped to provide 28 delay levels. 
        There is one final delivery exchange in addition to the delay-level exchanges. 
        When a message needs to be delayed, the value of the desired delay is first converted to seconds. 
        The binary representation of this value is used as part of the routing key when the message is sent to the delay-level exchanges. 
        
        The full routing key has the following format:
        N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.N.destination
        Where N is either 0 or 1, representing the delay value in binary, and destination is the name of endpoint the delayed message will be sent to.

        ===================== Delay levels
        Each exchange/queue pair that makes up a level represents one bit of the total delay value. 
        By having 28 of these levels, corresponding to 2^27 through 2^0, the delay infrastructure can delay a message for any value that can be represented by a 28-bit number. 
        With 28 total levels, the maximum delay value is 268,435,455 seconds, or about 8.5 years.

        A delay level is created by declaring a topic exchange that is bound to a queue with a routing key of 1, 
        and to the exchange corresponding to level - 1 with a routing key of 0. 
        
        The queue for the delay level is declared with an x-message-ttl value corresponding to 2^level seconds. 
        The queue is also declared with an x-dead-letter-exchange value corresponding to the level - 1 exchange, 
        so that when a message in the queue expires, it will be routed to the level - 1 exchange.

        */
        private void EnsureQueueExistingInfrastructure(string queueName, ILogger logger)
        {
            var cts = new CancellationTokenSource();
            var logProblemTask = new Task(async () =>
            {
                do
                {
                    await Task.Delay(10_000); // 10s
                    if (cts.IsCancellationRequested)
                        break;
                    logger.LogException(new TimeoutException("RabbitMq delay infrastructure is too long to create. If you see this error, go to RabbitMq dashboard and you should find a stopped queue."));
                }
                while (!cts.IsCancellationRequested);
            });

            _holder.DoWithChannel((channel) =>
            {
                _delayInfrastructure.Build(channel, queueName);
            });

            cts.Cancel(); // logProblemTask will end nicely, no need to wait for it

            try
            {
                //https://www.rabbitmq.com/ha.html
                /* EXAMPLE :
                 * PUT /api/policies/%2f/ha-two
                 * {"pattern":"^two\.", "definition":{"ha-mode":"exactly", "ha-params":2,"ha-sync-mode":"automatic"}}
                 * */
                var uriBuilder = new UriBuilder()
                {
                    Scheme = Uri.UriSchemeHttp,
                    Host = _settings.Host,
                    Port = _settings.ManagerPort,
                    Path = "/api/policies/%2f/ha-all",
                };

                var content = new StringContent($@"{{""pattern"":""."", ""definition"":{{""ha-mode"":""all"", ""ha-promote-on-failure"":""when-synced"", ""ha-promote-on-shutdown"":""when-synced"",""ha-sync-mode"":""automatic""}}}}");

                using (var response = _httpClient.PutAsync(uriBuilder.Uri, content).Result)
                {
                    response.EnsureSuccessStatusCode();
                }
            }
            catch (Exception ex)
            {
                _appContext.Logger.LogException(ex);
            }
        }



        public override void Push<T>(string queueName, IEnumerable<T> messageContents, TimeSpan? initialVisibilityDelay = default)
        {
            foreach (var content in messageContents)
            {
                var body = content.ToJsonToBytes(keepDictionaryKeyCase: true);

                var props = _holder.GetBasicProperties();
                if (content is IMessage message)
                {
                    props.CorrelationId = message.CorrelationId.ToString();
                    props.MessageId = message.MessageId.ToString();
                    props.Type = message.GetType().GetFriendlyName();
                }

                _appContext.Tracer.InjectProperties(props);

                _holder.DoWithChannel(channel =>
                {
                    if (initialVisibilityDelay.HasValue && initialVisibilityDelay.Value > TimeSpan.Zero)
                    {
                        var delayInSeconds = Math.Max(1, (int)initialVisibilityDelay.Value.TotalSeconds); // no less than 1
                        var routingKey = _delayInfrastructure.CalculateRoutingKey(delayInSeconds, ONLY_ADRESS, out var startingDelayLevel);
                        channel.BasicPublish(_delayInfrastructure.LevelName(queueName, startingDelayLevel), routingKey: routingKey, basicProperties: props, body: body, mandatory: true);
                    }
                    else
                    {
                        channel.BasicPublish(exchange: queueName, routingKey: queueName, basicProperties: props, body: body, mandatory: true);
                    }

                    // https://rianjs.net/2013/12/publisher-confirms-with-rabbitmq-and-c-sharp
                    // https://stackoverflow.com/questions/25882074/timeout-of-basicpublish-when-server-is-outofspace
                    channel.WaitForConfirmsOrDie(_defaultPublishConfirmTimeout);
                });
            }
        }

        public override Task PushAsync<T>(string queueName, IEnumerable<T> messageContents, TimeSpan? initialVisibilityDelay = null)
        {
            Push(queueName, messageContents, initialVisibilityDelay);
            return Task.CompletedTask;
        }

        public override Task PartitionedPushAsync<T>(string queueName, string partitionKey, IEnumerable<T> messageContents, TimeSpan? initialVisibilityDelay = null)
        {
            Push(queueName, messageContents, initialVisibilityDelay);
            return Task.CompletedTask;
        }


        public override void Dispose()
        {
            _httpClient?.Dispose();
            _holder?.Dispose();
        }
    }
}
