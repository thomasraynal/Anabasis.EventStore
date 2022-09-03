using Azure.Data.Tables;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Producer;
using Azure.Storage.Blobs;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Tests.Integration
{
    //https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/eventhub/Azure.Messaging.EventHubs.Processor/samples/Sample03_EventProcessorHandlers.md#common-guidance-for-handlers

    [TestFixture]
    public class TestEventHubBasics
    {
        [Test]
        public async Task ShouldPushAndSubscribeToMessages()
        {
            var connectionString = "Endpoint=sb://testveventhub.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=+nHacBC+Ix3bS/8VzAeFYw4NWut+k1Sm84tmLLDLkKQ=";
            var eventHubName = "test";

            var producerClient = new EventHubProducerClient(connectionString, eventHubName);

            var processorOptions = new EventProcessorClientOptions
            {
                LoadBalancingStrategy = LoadBalancingStrategy.Greedy,
                LoadBalancingUpdateInterval = TimeSpan.FromSeconds(10),
                PartitionOwnershipExpirationInterval = TimeSpan.FromSeconds(30),
                RetryOptions = new EventHubsRetryOptions
                {
                    Mode = EventHubsRetryMode.Exponential,
                    MaximumRetries = 5,
                    Delay = TimeSpan.FromMilliseconds(800),
                    MaximumDelay = TimeSpan.FromSeconds(10)
                }
            };


            using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();

            for (int i = 1; i <= 10; i++)
            {
                if (!eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes($"Event {i}"))))
                {
                    // if it is too large for the batch
                    throw new Exception($"Event {i} is too large for the batch and cannot be sent.");
                }

                await Application.SendHeartbeatAsync(args.CancellationToken);
            }

            try
            {
                // Use the producer client to send the batch of events to the event hub
                await producerClient.SendAsync(eventBatch);
                Console.WriteLine($"A batch of {10} events has been published.");
            }
            finally
            {
                await producerClient.DisposeAsync();
            }
        

            // Read from the default consumer group: $Default
            var consumerGroup = EventHubConsumerClient.DefaultConsumerGroupName;
            var ehubNamespaceConnectionString = "Endpoint=sb://testveventhub.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=+nHacBC+Ix3bS/8VzAeFYw4NWut+k1Sm84tmLLDLkKQ=";
       
            // Create a blob container client that the event processor will use 
            var storageClient = new BlobContainerClient("DefaultEndpointsProtocol=https;AccountName=thomastesteventhub;AccountKey=znN9CHoYO/LhsUntKrpSWVCmTL7g1Gz7aIssIhshIl8MEL1mHGGNYf3L+zVdqCaPOFfz8oNxdjH2+AStCJHu9g==;EndpointSuffix=core.windows.net",
                 "blob");

            var eventProcessorClientOptions = new EventProcessorClientOptions();

            // Create an event processor client to process events in the event hub
            var processor = new EventProcessorClient(storageClient, consumerGroup, ehubNamespaceConnectionString, eventHubName);

           
            // Register handlers for processing events and handling errors
            processor.ProcessEventAsync += ProcessEventHandler;
            processor.ProcessErrorAsync += ProcessErrorHandler;
            processor.PartitionInitializingAsync += InitializeEventHandler;
            processor.PartitionClosingAsync += CloseEventHandler;

            // Start the processing
            await processor.StartProcessingAsync();

            // Wait for 30 seconds for the events to be processed
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Stop the processing
            await processor.StopProcessingAsync();

        }


        static Task CloseEventHandler(PartitionClosingEventArgs args)
        {
            try
            {
                if (args.CancellationToken.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }

                string description = args.Reason switch
                {
                    ProcessingStoppedReason.OwnershipLost =>
                        "Another processor claimed ownership",

                    ProcessingStoppedReason.Shutdown =>
                        "The processor is shutting down",

                    _ => args.Reason.ToString()
                };

                Debug.WriteLine($"Closing partition: { args.PartitionId }");
                Debug.WriteLine($"\tReason: { description }");
            }
            catch
            {
                // Take action to handle the exception.
                // It is important that all exceptions are
                // handled and none are permitted to bubble up.
            }

            return Task.CompletedTask;
        }


        static Task InitializeEventHandler(PartitionInitializingEventArgs args)
        {
            try
            {
                if (args.CancellationToken.IsCancellationRequested)
                {
                    return Task.CompletedTask;
                }

                Debug.WriteLine($"Initialize partition: { args.PartitionId }");

                // If no checkpoint was found, start processing
                // events enqueued now or in the future.

                EventPosition startPositionWhenNoCheckpoint =
                    EventPosition.FromEnqueuedTime(DateTimeOffset.UtcNow);

                args.ch
                args.DefaultStartingPosition = startPositionWhenNoCheckpoint;
            }
            catch
            {
                // Take action to handle the exception.
                // It is important that all exceptions are
                // handled and none are permitted to bubble up.
            }

            return Task.CompletedTask;
        }

        static async Task ProcessEventHandler(ProcessEventArgs eventArgs)
        {
            // Write the body of the event to the console window
            Console.WriteLine("\tReceived event: {0}", Encoding.UTF8.GetString(eventArgs.Data.Body.ToArray()));
            
            // Update checkpoint in the blob storage so that the app receives only new events the next time it's run
            await eventArgs.UpdateCheckpointAsync(eventArgs.CancellationToken);
        }

        static Task ProcessErrorHandler(ProcessErrorEventArgs eventArgs)
        {
            // Write details about the error to the console window
            Console.WriteLine($"\tPartition '{ eventArgs.PartitionId}': an unhandled exception was encountered. This was not expected to happen.");
            Console.WriteLine(eventArgs.Exception.Message);
            return Task.CompletedTask;
        }

        static async Task<CloudTable> PrepareMonitoringCloudTable(string storageConnectionString, string monitoringTableName)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(monitoringTableName);

            tableClient.DefaultRequestOptions.RetryPolicy = StorageHelper.GetDefaultRetryPolicy();

            await table.CreateIfNotExistsAsync();

            return table;
        }

        static async Task SaveCheckPointInformationAsync(TableClient tableClient, PartitionContext context, EventData last, string hostname, string @namespace)
        {
            TrackingRecorderCheckpointStatus statusEntity;

            var partitionKey = @namespace + "-" + context.EventHubName + "-" + context.ConsumerGroup;
            var rowKey = context.PartitionId;

            if (last == null)
            {
                statusEntity = new TrackingRecorderCheckpointStatus()
                {
                    PartitionKey = partitionKey,
                    ConsumerGroupName = context.ConsumerGroup,
                    RowKey = rowKey,
                    HostName = hostname,
                    LastCheckedUtcDate = DateTime.UtcNow,
                };
            }
            else
            {
                long messageRate = 0;
                TimeSpan duration = TimeSpan.Zero;
                var lastCheckedUtcDate = DateTime.UtcNow;

                var tableResult = await tableClient.GetEntityAsync<FullTrackingRecorderCheckpointStatus>(partitionKey, rowKey);

                long? previousSequenceNumber = null;
                DateTime? previousCheckedUtcDate = null;

                if (tableResult != null)
                {
                    var x = (FullTrackingRecorderCheckpointStatus)tableResult;
                    previousCheckedUtcDate = x.LastCheckedUtcDate;
                    previousSequenceNumber = x.SequenceNumber;
                }

                if (previousCheckedUtcDate.HasValue && previousSequenceNumber.HasValue)
                {
                    messageRate = last.SequenceNumber - previousSequenceNumber.Value;
                    duration = lastCheckedUtcDate - previousCheckedUtcDate.Value;
                }

                object eventIdOut;
                var eventId = Guid.Empty;

                if (!(
                    last.Properties.TryGetValue(EventHubsConstants.EventIdNameInEventProperty, out eventIdOut) &&
                    Guid.TryParse(eventIdOut.ToString(), out eventId))
                    )
                {
                    eventId = Guid.Empty;
                }

                statusEntity = new FullTrackingRecorderCheckpointStatus()
                {
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    ConsumerGroupName = context.ConsumerGroup,
                    HostName = hostname,
                    LastEnqueuedUtcDate = last.EnqueuedTime.UtcDateTime,
                    LastCheckedUtcDate = lastCheckedUtcDate,
                    SequenceNumber = last.SequenceNumber,
                    PreviousSequenceNumber = previousSequenceNumber,
                    PreviousCheckedUtcDate = previousCheckedUtcDate,
                    MessageRateSincePreviousCheck = messageRate,
                    DurationSincePreviousCheck = duration.ToString("c"),
                    LastEventId = eventId
                };
            }


            await tableClient.UpsertEntityAsync(statusEntity);
        }

    }
}
