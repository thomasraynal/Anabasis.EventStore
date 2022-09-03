using Anabasis.Common;
using Anabasis.EventHubs.Shared;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    public class EventHubBus : IEventHubBus
    {
     
        class EventHubSubscriber
        {
            public EventHubSubscriber(Guid subscriberId, Func<IEvent[]> onEventsReceived)
            {
                SubscriberId = subscriberId;
                OnEventsReceived = onEventsReceived;
            }

            public Guid SubscriberId { get; set; }
            public Func<IEvent[]> OnEventsReceived { get; set; }
        }

        private readonly EventHubOptions _eventHubOptions;
        private readonly EventProcessorOptions _eventProcessorOptions;
        private readonly EventHubProducerClientOptions _eventHubProducerClientOptions;
        private readonly ILogger<EventHubBus>? _logger;
        private readonly ISerializer _serializer;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly AnabasisEventHubProcessor _eventHubProcessorClient;
        private readonly Dictionary<Guid,EventHubSubscriber> _eventHubSubscribers;

        public EventHubBus(EventHubOptions eventHubConnectionOptions,
            EventProcessorOptions eventProcessorOptions,
            EventHubProducerClientOptions eventHubProducerClientOptions,
            ISerializer serializer,
            ILoggerFactory? loggerFactory = null)
        {
            _eventHubOptions = eventHubConnectionOptions;
            _eventProcessorOptions = eventProcessorOptions;
            _eventHubProducerClientOptions = eventHubProducerClientOptions;

            _eventHubSubscribers = new Dictionary<Guid, EventHubSubscriber>();
            _logger = loggerFactory?.CreateLogger<EventHubBus>();
            _serializer = serializer;

            _eventHubProducerClient = GetEventHubProducerClient();
            _eventHubProcessorClient = GetEventProcessorClient();

            BusId = $"{nameof(EventHubBus)}_{Guid.NewGuid()}";

            ConnectionStatusMonitor = new EventHubConnectionStatusMonitor();

        }

        public string BusId { get; }

        public IConnectionStatusMonitor ConnectionStatusMonitor { get; }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext? context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _eventHubProducerClient.GetEventHubPropertiesAsync(cancellationToken);

                return HealthCheckResult.Healthy($"EventHub bus {_eventHubOptions.Namespace}.{_eventHubOptions.HubName} is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"EventHub bus {_eventHubOptions.Namespace}.{_eventHubOptions.HubName} is unhealthy - {ex.Message}", ex);
            }

        }

        public void Dispose()
        {
            _eventHubProducerClient.CloseAsync().Wait();
            _eventHubProducerClient.DisposeAsync().AsTask().Wait();

            _eventHubProcessorClient.StopProcessing();

        }

        public async Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            var waitUntilMax = DateTime.UtcNow.Add(null == timeout ? Timeout.InfiniteTimeSpan : timeout.Value);
            var isConnected = false;

            while (!isConnected && DateTime.UtcNow > waitUntilMax)
            {
                var healthCheck = await CheckHealthAsync(null);

                isConnected = healthCheck.Status == HealthStatus.Healthy;

                await Task.Delay(100);
            }
        }

        private AnabasisEventHubProcessor GetEventProcessorClient()
        {
            var eventHubConnectionString = _eventHubOptions.GetConnectionString();
            var storageConnectionString = _eventHubOptions.EventHubConsumerSettings.GetConnectionString();
          
            var storageClient = new BlobContainerClient(storageConnectionString, _eventHubOptions.EventHubConsumerSettings.BlobContainerName);
            
            var checkpointStore = new BlobCheckpointStore(storageClient);
    

            var anabasisEventHubProcessor = new AnabasisEventHubProcessor(
                checkpointStore,
                _eventHubOptions.MaximumBatchSize,
                _eventHubOptions.ConsumerGroup,
                eventHubConnectionString,
               _eventHubOptions.HubName,
               _eventProcessorOptions);

          //  return new EventProcessorClient(storageClient, _eventHubConnectionOptions.ConsumerGroup, eventHubConnectionString, _eventHubConnectionOptions.HubName, _eventProcessorOptions);
        }

        private EventHubProducerClient GetEventHubProducerClient()
        {
            var connectionString = _eventHubOptions.GetConnectionString();

            return new EventHubProducerClient(connectionString, _eventHubOptions.HubName, _eventHubProducerClientOptions);

        }
        public async Task Emit(IEvent @event, SendEventOptions? sendEventOptions = null, CancellationToken cancellationToken = default)
        {
          
            var eventHubMessage = new EventHubMessage(Guid.NewGuid(), @event);
            var serializedMessage = _serializer.SerializeObject(eventHubMessage);
            var eventData = new EventData(serializedMessage);

            await _eventHubProducerClient.SendAsync(new[] { eventData }, sendEventOptions, cancellationToken);
        }

        public async Task Emit(IEnumerable<IEvent> eventBatch, CreateBatchOptions? createBatchOptions = null, CancellationToken cancellationToken = default)
        {
            using var eventDataBatch = await _eventHubProducerClient.CreateBatchAsync(createBatchOptions, cancellationToken);

            foreach(var @event in eventBatch)
            {
                var eventHubMessage = new EventHubMessage(Guid.NewGuid(), @event);
                var serializedMessage = _serializer.SerializeObject(eventHubMessage);
                var eventData = new EventData(serializedMessage);

                if (!eventDataBatch.TryAdd(eventData))
                {
                    var invalidOperationException = new InvalidOperationException($"Event {@event.EventId} -  {@event.Name} cannot be processsed by {nameof(EventDataBatch)}");

                    invalidOperationException.Data["event"] = @event.ToJson();

                    throw invalidOperationException;
                }

            }

            await _eventHubProducerClient.SendAsync(eventDataBatch, cancellationToken);
        }

        public void UnSubscribeToEventHub(Guid subscriptionId)
        {
            if (!_eventHubSubscribers.ContainsKey(subscriptionId))
            {
                throw new InvalidOperationException($"Cannot remove subscription {subscriptionId} - subscription doesn't exist.");
            }

            _eventHubSubscribers.Remove(subscriptionId);
        }

        public Guid SubscribeToEventHub(Func<IEvent[]> onEventsReceived)
        {
            var subscription = new EventHubSubscriber(Guid.NewGuid(), onEventsReceived);

            _eventHubSubscribers.Add(subscription.SubscriberId, subscription);

            return subscription.SubscriberId;
        }
    }
}
