﻿using Anabasis.Common;
using Anabasis.Common.Utilities;
using Anabasis.EventHubs.Bus;
using Anabasis.EventHubs.Shared;
using Azure;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Azure.Messaging.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    public class AnabasisEventHubProcessor : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>
    {

        class EventHubSubscriber
        {
            public EventHubSubscriber(Guid subscriberId, Func<IMessage[], CancellationToken, Task> onEventsReceived)
            {
                SubscriberId = subscriberId;
                OnMessagesReceived = onEventsReceived;
            }

            public Guid SubscriberId { get; set; }
            public Func<IMessage[], CancellationToken, Task> OnMessagesReceived { get; set; }
        }

        private Dictionary<Guid, EventHubSubscriber> _eventHubSubscribers;
        private ILogger<EventHubBus>? _logger;
        private ISerializer _serializer;
        private EventHubOptions _eventHubOptions;
        private IKillSwitch _killSwitch;
        private IEventHubPartitionMonitoring _eventHubPartitionMonitoring;
        private DateTime _lastCheckPointUtc;

#nullable disable

        public AnabasisEventHubProcessor(ILoggerFactory loggerFactory, EventHubOptions eventHubOptions, ISerializer serializer, IKillSwitch killSwitch, IEventHubPartitionMonitoring eventHubPartitionMonitoring)
        {
            Initialize(loggerFactory, eventHubOptions, serializer, killSwitch, eventHubPartitionMonitoring);
        }

        public AnabasisEventHubProcessor(ILoggerFactory loggerFactory, EventHubOptions eventHubOptions, ISerializer serializer, IKillSwitch killSwitch, IEventHubPartitionMonitoring eventHubPartitionMonitoring, CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string connectionString, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, connectionString, options)
        {
            Initialize(loggerFactory, eventHubOptions, serializer, killSwitch, eventHubPartitionMonitoring);
        }

        public AnabasisEventHubProcessor(ILoggerFactory loggerFactory, EventHubOptions eventHubOptions, ISerializer serializer, IKillSwitch killSwitch, IEventHubPartitionMonitoring eventHubPartitionMonitoring, CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string connectionString, string eventHubName, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, connectionString, eventHubName, options)
        {
            Initialize(loggerFactory, eventHubOptions, serializer, killSwitch, eventHubPartitionMonitoring);
        }

        public AnabasisEventHubProcessor(ILoggerFactory loggerFactory, EventHubOptions eventHubOptions, ISerializer serializer, IKillSwitch killSwitch, IEventHubPartitionMonitoring eventHubPartitionMonitoring, CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string fullyQualifiedNamespace, string eventHubName, AzureNamedKeyCredential credential, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, fullyQualifiedNamespace, eventHubName, credential, options)
        {
            Initialize(loggerFactory, eventHubOptions, serializer, killSwitch, eventHubPartitionMonitoring);
        }

        public AnabasisEventHubProcessor(ILoggerFactory loggerFactory, EventHubOptions eventHubOptions, ISerializer serializer, IKillSwitch killSwitch, IEventHubPartitionMonitoring eventHubPartitionMonitoring, CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string fullyQualifiedNamespace, string eventHubName, AzureSasCredential credential, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, fullyQualifiedNamespace, eventHubName, credential, options)
        {
            Initialize(loggerFactory, eventHubOptions, serializer, killSwitch, eventHubPartitionMonitoring);
        }

        public AnabasisEventHubProcessor(ILoggerFactory loggerFactory, EventHubOptions eventHubOptions, ISerializer serializer, IKillSwitch killSwitch, IEventHubPartitionMonitoring eventHubPartitionMonitoring, CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string fullyQualifiedNamespace, string eventHubName, TokenCredential credential, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, fullyQualifiedNamespace, eventHubName, credential, options)
        {
            Initialize(loggerFactory, eventHubOptions, serializer, killSwitch, eventHubPartitionMonitoring);
        }

#nullable enable

        private void Initialize(ILoggerFactory? loggerFactory, EventHubOptions eventHubOptions, ISerializer serializer, IKillSwitch killSwitch, IEventHubPartitionMonitoring eventHubPartitionMonitoring)
        {
            _eventHubSubscribers = new Dictionary<Guid, EventHubSubscriber>();
            _logger = loggerFactory?.CreateLogger<EventHubBus>();
            _eventHubOptions = eventHubOptions;
            _serializer = serializer;
            _lastCheckPointUtc = DateTime.UtcNow;
            _killSwitch = killSwitch;
            _eventHubPartitionMonitoring = eventHubPartitionMonitoring;
        }

        internal int SubscribersCount => _eventHubSubscribers.Count;

        internal void UnSubscribeFromEventHub(Guid subscriptionId)
        {
            if (!_eventHubSubscribers.ContainsKey(subscriptionId))
            {
                throw new InvalidOperationException($"Cannot remove subscription {subscriptionId} - subscription doesn't exist.");
            }

            _eventHubSubscribers.Remove(subscriptionId);
        }

        internal Guid SubscribeToEventHub(Func<IMessage[], CancellationToken, Task> onEventsReceived)
        {
            var subscription = new EventHubSubscriber(Guid.NewGuid(), onEventsReceived);

            _eventHubSubscribers.Add(subscription.SubscriberId, subscription);

            return subscription.SubscriberId;
        }

        protected async override Task<EventProcessorCheckpoint> GetCheckpointAsync(string partitionId, CancellationToken cancellationToken)
        {
            var checkpoint = await base.GetCheckpointAsync(partitionId, cancellationToken);

            if (checkpoint == null)
            {

                var startingTime = _eventHubOptions.EventHubProcessingStartTimeUtcOverride ?? DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(5));

                checkpoint = new EventProcessorCheckpoint
                {
                    FullyQualifiedNamespace = this.FullyQualifiedNamespace,
                    EventHubName = EventHubName,
                    ConsumerGroup = ConsumerGroup,
                    PartitionId = partitionId,
                    StartingPosition = EventPosition.FromEnqueuedTime(startingTime)
                };
            }

            return checkpoint;
        }

        protected override Task OnInitializingPartitionAsync(EventProcessorPartition partition, CancellationToken cancellationToken)
        {

            _logger?.LogInformation($"Initializing partition {partition.PartitionId}");


            return base.OnInitializingPartitionAsync(partition, cancellationToken);
        }

        protected override Task OnPartitionProcessingStoppedAsync(EventProcessorPartition partition, ProcessingStoppedReason reason, CancellationToken cancellationToken)
        {

            _logger?.LogInformation($"Stopped processing partition {partition.PartitionId} - {reason}");

            return base.OnPartitionProcessingStoppedAsync(partition, reason, cancellationToken);
        }

        protected override Task OnProcessingErrorAsync(Exception exception, EventProcessorPartition partition, string operationDescription, CancellationToken cancellationToken)
        {
            exception.Data["eventProcessorPartition"] = partition;
            exception.Data["operationDescription"] = operationDescription;

            _logger?.LogError(exception, $"An error occured during the delivering of a message");

            if (_eventHubOptions.DoAppCrashOnFailure)
            {
                _killSwitch.KillProcess(exception);
            }

            return Task.CompletedTask;
        }

        private async Task WaitForTheMessageBatchToBeAcknowledged(EventHubMessage[] eventHubMessage, TimeSpan? timeout = null)
        {
            timeout ??= TimeSpan.FromMinutes(1);

            var now = DateTime.UtcNow;

            var isEventBatchAcknowledged = eventHubMessage.All(message => message.IsAcknowledged);

            while (!isEventBatchAcknowledged && DateTime.UtcNow < now.Add(timeout.Value))
            {
                isEventBatchAcknowledged = eventHubMessage.All(message => message.IsAcknowledged);

                //todo: use observable
                await Task.Delay(100);
            }

            if (!isEventBatchAcknowledged)
            {
                throw new TimeoutException($"Unable to acknowledge the message group in due time - timeout :{timeout}");
            }
        }

        protected override async Task OnProcessingEventBatchAsync(IEnumerable<EventData> events, EventProcessorPartition partition, CancellationToken cancellationToken)
        {

            try
            {

                var eventStream = events.ToArray();

                if (eventStream.Length == 0) return;

                var messages = eventStream.Select(eventData =>
                {
                    var eventTypeAsString = eventData.Properties[EventHubsConstants.EventTypeNameInEventProperty]?.ToString();
                    var messageIdAsString = eventData.Properties[EventHubsConstants.MessageIdNameInEventProperty].ToString();

                    if (string.IsNullOrEmpty(eventTypeAsString))
                    {
                        throw new InvalidOperationException($"{EventHubsConstants.EventTypeNameInEventProperty} value is null or undefined");
                    }

                    if (string.IsNullOrEmpty(messageIdAsString))
                    {
                        throw new InvalidOperationException($"{EventHubsConstants.MessageIdNameInEventProperty} value is null or undefined");
                    }

                    var @event = (IEvent)_serializer.DeserializeObject(eventData.EventBody.ToArray(), eventTypeAsString.GetTypeFromReadableName());

                    return new EventHubMessage(Guid.Parse(messageIdAsString), @event);

                }).ToArray();

                await Task.WhenAll(_eventHubSubscribers.Values.Select(async eventHubSubscriber =>
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    await eventHubSubscriber.OnMessagesReceived(messages, cancellationToken);

                }));

            
                var doCheckPoint = DateTime.UtcNow >= _lastCheckPointUtc.Add(_eventHubOptions.CheckPointPeriod);

                if (doCheckPoint)
                {
                    await WaitForTheMessageBatchToBeAcknowledged(messages);
                }

                if (doCheckPoint && !cancellationToken.IsCancellationRequested)
                {
                    var lastEvent = eventStream.Last();

                    await UpdateCheckpointAsync(
                        partition.PartitionId,
                        lastEvent.Offset,
                        lastEvent.SequenceNumber,
                        cancellationToken);

                    await _eventHubPartitionMonitoring.SaveCheckPointMonitoring(partition, eventStream.Last(), "-", _eventHubOptions.EventHubNamespace);
                }

            }
            catch (Exception ex)
            {

                _logger?.LogError(ex.GetActualException(), $"An error occured during the delivery of a message");

                if (_eventHubOptions.DoAppCrashOnFailure)
                {
                    _killSwitch.KillProcess(ex);
                }

            }
        }
    }
}
