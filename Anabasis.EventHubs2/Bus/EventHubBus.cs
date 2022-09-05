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
using Anabasis.Common.Utilities;
using Anabasis.EventHubs.Bus;

namespace Anabasis.EventHubs
{
 
    //todo=> lease container name
    //todo=> monitoring table
    //todo=> check prod setup


    public class EventHubBus : IEventHubBus
    {

        private readonly ILoggerFactory? _loggerFactory;
        private readonly ILogger<EventHubBus>? _logger;
        private readonly ISerializer _serializer;
        private readonly IKillSwitch _killSwitch;

        private readonly EventHubOptions _eventHubOptions;
        private readonly EventProcessorOptions _eventProcessorOptions;
        private readonly EventHubProducerClientOptions _eventHubProducerClientOptions;
        private readonly EventHubProducerClient _eventHubProducerClient;
        private readonly AnabasisEventHubProcessor _eventHubProcessorClient;

        public EventHubBus(EventHubOptions eventHubConnectionOptions,
            EventProcessorOptions eventProcessorOptions,
            EventHubProducerClientOptions eventHubProducerClientOptions,
            ISerializer serializer,
            IEventHubPartitionMonitoring? eventHubPartitionMonitoring = null,
            IKillSwitch? killSwitch = null,
            ILoggerFactory? loggerFactory = null)
        {
            _eventHubOptions = eventHubConnectionOptions;
            _eventProcessorOptions = eventProcessorOptions;
            _eventHubProducerClientOptions = eventHubProducerClientOptions;
            eventHubPartitionMonitoring = eventHubPartitionMonitoring ?? new EventHubPartitionMonitoring(_eventHubOptions);

            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<EventHubBus>();
            _serializer = serializer;
            _killSwitch = killSwitch ?? new KillSwitch();

            _eventHubProducerClient = GetEventHubProducerClient();
            _eventHubProcessorClient = GetEventProcessorClient(eventHubPartitionMonitoring);

            BusId = $"{nameof(EventHubBus)}_{Guid.NewGuid()}";

            ConnectionStatusMonitor = new EventHubConnectionStatusMonitor();

        }

        public string BusId { get; }
        public bool IsProcessing { get; private set; }

        public IConnectionStatusMonitor ConnectionStatusMonitor { get; }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext? context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _eventHubProducerClient.GetEventHubPropertiesAsync(cancellationToken);

                return HealthCheckResult.Healthy($"EventHub bus {_eventHubOptions.EventHubNamespace}.{_eventHubOptions.EventHubName} is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"EventHub bus {_eventHubOptions.EventHubNamespace}.{_eventHubOptions.EventHubName} is unhealthy - {ex.Message}", ex);
            }

        }

        public void Dispose()
        {
            _eventHubProducerClient.CloseAsync().Wait();
            _eventHubProducerClient.DisposeAsync().AsTask().Wait();
            
            EnsureStopProcessing().Wait();

        }

        private async Task EnsureStartProcessing()
        {
            if (IsProcessing) return;

            await _eventHubProcessorClient.StartProcessingAsync();

            IsProcessing = true;
        }

        private async Task EnsureStopProcessing()
        {
            if (!IsProcessing) return;

            await _eventHubProcessorClient.StartProcessingAsync();

            IsProcessing = false;
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

        private AnabasisEventHubProcessor GetEventProcessorClient(IEventHubPartitionMonitoring eventHubPartitionMonitoring)
        {
            var eventHubConnectionString = _eventHubOptions.GetEventHubConnectionString();
            var storageConnectionString = _eventHubOptions.GetCheckpointStorageConnectionString();
          
            var storageClient = new BlobContainerClient(storageConnectionString, _eventHubOptions.CheckpointBlobContainerName);

            storageClient.CreateIfNotExists();

            var checkpointStore = new BlobCheckpointStore(storageClient);
    
            var anabasisEventHubProcessor = new AnabasisEventHubProcessor(
                _loggerFactory,
                _eventHubOptions,
                _serializer,
                _killSwitch,
                eventHubPartitionMonitoring,
                checkpointStore,
                _eventHubOptions.EventHubMaximumBatchSize,
                _eventHubOptions.EventHubConsumerGroup,
                eventHubConnectionString,
               _eventHubOptions.EventHubName,
               _eventProcessorOptions);

            return anabasisEventHubProcessor;
        }

        private EventHubProducerClient GetEventHubProducerClient()
        {
            var connectionString = _eventHubOptions.GetEventHubConnectionString();

            return new EventHubProducerClient(connectionString, _eventHubOptions.EventHubName, _eventHubProducerClientOptions);

        }

        private EventData CreateEventData(IEvent @event)
        {
            var serializedEvent = _serializer.SerializeObject(@event);

            var eventData = new EventData(serializedEvent);
            eventData.Properties[EventHubsConstants.EventTypeNameInEventProperty] = @event.GetReadableNameFromType();
            eventData.Properties[EventHubsConstants.EventIdNameInEventProperty] = @event.EventId;
            eventData.Properties[EventHubsConstants.MessageIdNameInEventProperty] = Guid.NewGuid().ToString();
            eventData.ContentType = _serializer.ContentMIMEType;

            return eventData;
        }

        public async Task Emit(IEvent @event, SendEventOptions? sendEventOptions = null, CancellationToken cancellationToken = default)
        {

            var eventData = CreateEventData(@event);

            await _eventHubProducerClient.SendAsync(new[] { eventData }, sendEventOptions, cancellationToken);
        }

        public async Task Emit(IEnumerable<IEvent> eventBatch, CreateBatchOptions? createBatchOptions = null, CancellationToken cancellationToken = default)
        {
            using var eventDataBatch = await _eventHubProducerClient.CreateBatchAsync(createBatchOptions, cancellationToken);

            foreach(var @event in eventBatch)
            {

                var eventData = CreateEventData(@event);

                if (!eventDataBatch.TryAdd(eventData))
                {
                    var invalidOperationException = new InvalidOperationException($"Event {@event.EventId} -  {@event.Name} cannot be processsed by {nameof(EventDataBatch)}");

                    invalidOperationException.Data["event"] = @event.ToJson();

                    throw invalidOperationException;
                }

            }

            await _eventHubProducerClient.SendAsync(eventDataBatch, cancellationToken);
        }

        public async Task UnSubscribeToEventHub(Guid subscriptionId)
        {
            _eventHubProcessorClient.UnSubscribeToEventHub(subscriptionId);

            if (_eventHubProcessorClient.SubscribersCount == 0)
            {
                await EnsureStopProcessing();
            }

        }

        public async Task<Guid> SubscribeToEventHub(Func<IMessage[],CancellationToken, Task> onEventsReceived)
        {
            var subscriptionId = _eventHubProcessorClient.SubscribeToEventHub(onEventsReceived);

            await EnsureStartProcessing();

            return subscriptionId;
        }
    }
}
