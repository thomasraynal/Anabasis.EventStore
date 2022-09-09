using Anabasis.Common;
using Anabasis.Common.Configuration;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using Azure.Messaging.EventHubs.Producer;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Tests.Integration
{
    public class EventHubTestEvent : BaseEvent
    {
        public long SequenceNumber { get; }

        public EventHubTestEvent(long sequenceNumber, Guid correlationId, string streamId) : base(streamId, correlationId)
        {
            SequenceNumber = sequenceNumber;
        }
    }

    [TestFixture]
    public class TestEventHubBasics
    {
        [Ignore("integration")]
        [Test]
        public async Task ShouldPushAndSubscribeToMessagesInWorker()
        {
            var index = 0;

            var workerConfiguration = new WorkerConfiguration()
            {
                DispatcherCount = 2,
            };

            var testEventHubWorker = new TestEventHubWorker(workerConfiguration);

            var eventHubOptions = new EventHubOptions()
            {
                CheckPointPeriod = TimeSpan.FromSeconds(3),
                EventHubMaximumBatchSize = 3,
                EventHubNamespace = "",
                EventHubName = "",
                CheckpointBlobContainerName = "",
                EventHubSharedAccessKey = "",
                EventHubSharedAccessKeyName = "",
                CheckpointStoreAccountKey = "",
                CheckpointStoreAccountName = "",

            };

            var eventHubProducerClientOptions = new EventHubProducerClientOptions();

            var eventProcessorClientOptions = new EventProcessorOptions
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

            var eventHubBus = new EventHubBus(eventHubOptions, eventProcessorClientOptions, eventHubProducerClientOptions);

            await testEventHubWorker.ConnectTo(eventHubBus);

            await testEventHubWorker.WaitUntilConnected();

            await testEventHubWorker.SubscribeToEventHub();

            var messages = Enumerable.Range(0, 5).Select(_ => new EventHubTestEvent(index++, Guid.NewGuid(), "eventhub-test"));

            while (true)
            {
                await eventHubBus.Emit(messages);

                await Task.Delay(2000);
            }


        }

        [Ignore("integration")]
        [Test]
        public async Task ShouldPushAndSubscribeToMessages()
        {

            var eventHubOptions = new EventHubOptions()
            {
                EventHubNamespace = "",
                EventHubName = "",
                CheckpointBlobContainerName = "",
                EventHubSharedAccessKey = "",
                EventHubSharedAccessKeyName = "",
                CheckpointStoreAccountKey = "",
                CheckpointStoreAccountName = "",

            };

            var eventHubProducerClientOptions = new EventHubProducerClientOptions();

            var eventProcessorClientOptions = new EventProcessorOptions
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

            var eventHubBus = new EventHubBus(eventHubOptions, eventProcessorClientOptions, eventHubProducerClientOptions);

            var eventHubConnectionStatusMonitor = new EventHubConnectionStatusMonitor(eventHubBus);

            await Task.Delay(5000);

            var disposable = eventHubBus.SubscribeToEventHub().Subscribe(obs =>
            {
                foreach (var message in obs.messages)
                {
                    Debug.WriteLine(message.MessageId);
                }

            }, onError: (ex) =>
            {
                Debug.WriteLine(ex.Message);

            });

            var index = 0;

            var messages = Enumerable.Range(0, 5).Select(_ => new EventHubTestEvent(index++,Guid.NewGuid(), "eventhub-test"));

            Debug.WriteLine("******************");

            await eventHubBus.Emit(messages);

            await Task.Delay(2000);

            Debug.WriteLine("******************");

            await eventHubBus.Emit(messages);

            await Task.Delay(2000);

            disposable.Dispose();

        }

    }
}
