using BeezUP2.Framework.EventSourcing;
using BeezUP2.Framework.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;
using BeezUP2.Framework.Configuration;
using System.Reactive;
using System.Reactive.Disposables;
using BeezUP2.Framework.FileStorage;
using System.Reflection;
using BeezUP2.Framework.Messaging;
using System.Reactive.Concurrency;
using System.Threading;
using BeezUP2.Framework.Application;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using BeezUP2.Framework.FileStorage.Azure;
using BeezUP2.Framework.Insights;
using System.Runtime.CompilerServices;
using Microsoft.WindowsAzure.Storage.RetryPolicies;

namespace BeezUP2.Framework.EventHubs
{
    public static class EventHubsHelper
    {
        public readonly static TimeSpan DefaultCheckpointPeriods = TimeSpan.FromSeconds(15);
        public readonly static TimeSpan SpinwaitPeriod = TimeSpan.FromTicks(DefaultCheckpointPeriods.Ticks / 5);

        internal const long DEFAULT_CHECKPOINT_VALUE = -42;

        public static BeezUPRecordedEvent ToBeezUPRecordedEvent(this EventData e, string streamName, out (Guid eventId, string eventTypeName) eventInfos)
        {
            var props = e.Properties;

            eventInfos = default((Guid, string));

            if (
                !props.TryGetValue(EventHubsConstants.EventIdNameInEventProperty, out var eventIdValue) ||
                !Guid.TryParse(eventIdValue.ToString(), out eventInfos.eventId) ||
                !props.TryGetValue(EventHubsConstants.EventTypeNameInEventProperty, out eventInfos.eventTypeName)
                )
                return null;

            var data = e.Body.Array;

            object blobUri;
            bool isZipped;
            object oIsZipped;

            if (props.TryGetValue(EventHubsConstants.BlobUriInEventProperty, out blobUri) && blobUri is Uri)
            {
                using (var http = new HttpClient())
                {
                    try
                    {
                        data = http.GetByteArrayAsync(blobUri as Uri).Result;
                    }
                    catch (Exception ex)
                    {
                        var goodEx = ex.GetTheGoodException() as HttpRequestException;
                        if (goodEx.Message.Contains("404"))
                            return null;
                        else
                            throw;
                    }
                }
            }
            else if (props.TryGetValue(EventHubsConstants.IsZippedInEventProperty, out oIsZipped)
                    && oIsZipped != null
                    && bool.TryParse(oIsZipped.ToString(), out isZipped)
                    && isZipped)
            {
                data = data.Unzip();
            }

            byte[] metadata = props.ToJsonToBytes();

            var recordedEvent = new BeezUPRecordedEvent(
                null,
                null,
                streamName,
                eventInfos.eventId,
                e.SystemProperties?.SequenceNumber ?? 0,
                eventInfos.eventTypeName,
                data,
                metadata,
                true,
                e.SystemProperties?.EnqueuedTimeUtc ?? DateTime.UtcNow,
                e.SystemProperties?.SequenceNumber ?? 0
                );

            return recordedEvent;
        }

        public static EventData GetEventData<T>(T message, IMessageSerializer serializer, Func<DateTime, IFileStorageProvider> bigMessageStorageProviderFactory = null)
        {
            string str = serializer.SerializeToString(message);

            var eventName = message.GetType().GetFriendlyName();

            var byteArray = str.ToBytes();

            bool isZipped = false;
            if (byteArray.Length >= MessageConstants.MessageSizeToZip)
            {
                byteArray = byteArray.Zip(CompressionKind.FastLZ);
                isZipped = true;
            }

            Uri blobUri = null;
            if (byteArray.Length >= MessageConstants.MessageSizeToZip && bigMessageStorageProviderFactory != null)
            {
                var now = DateTime.UtcNow;
                var storageProvider = bigMessageStorageProviderFactory(now);
                var file = storageProvider.GetFile(GuiDate.NewGuid(DateTime.UtcNow).ToString() + ".json");
                file.WriteTextAsync(str).Wait();
                blobUri = file.Uri;
                byteArray = new byte[0];
            }
            bool isBlob = blobUri != null;

            var eventData = new EventData(byteArray); // { PartitionKey = partitionKey };
            eventData.Properties.Add(EventHubsConstants.EventTypeNameInEventProperty, eventName);
            eventData.Properties.Add(EventHubsConstants.IsZippedInEventProperty, isZipped);

            var trueMessage = message as IMessage;
            if (trueMessage != null)
                eventData.Properties.Add(EventHubsConstants.EventIdNameInEventProperty, trueMessage.MessageId.ToString());

            if (isBlob)
                eventData.Properties.Add(EventHubsConstants.BlobUriInEventProperty, blobUri);

            return eventData;
        }

        #region EventProcessorHost

        public static EventHubProcessorHostParameters ToEventProcessHostParameters(this EventHubConnectionSettings connectionSettings, BeezUPAppContext appContext, EventHubConsumerSettings eventHubConsumerSettings, string consumerGroupName = null, int maxBatchSize = EventHubProcessorHostParameters.DEFAULT_MAX_BATCH_SIZE)
        {
            return new EventHubProcessorHostParameters(
                connectionSettings,
                appContext.MachineName,
                consumerGroupName ?? appContext.ApplicationName.FullName,
                eventHubConsumerSettings,
                maxBatchSize
                );
        }



        public static string GetLeaseContainerName(EventHubProcessorHostParameters hostParameters)
            => $"eh-{hostParameters.Connection.Namespace}-{hostParameters.Connection.HubName}-procs".ToLower();

        public static EventProcessorHost ToEventProcessorHost(this EventHubProcessorHostParameters hostParameters)
        {
            var eventHubConsumerSettings = hostParameters.EventHubConsumerSettings;

            var processor = new EventProcessorHost(
                $"{hostParameters.EventProcessorHostName}-{Guid.NewGuid()}",
                hostParameters.Connection.HubName,
                hostParameters.ConsumerGroupName,
                hostParameters.Connection.GetConnectionString(),
                eventHubConsumerSettings.BlobStorage.GetStorageConnectionString(),
                GetLeaseContainerName(hostParameters)
            );

            processor.PartitionManagerOptions = new PartitionManagerOptions
            {
                LeaseDuration = eventHubConsumerSettings.LeaseDurationInSec.HasValue ? TimeSpan.FromSeconds(eventHubConsumerSettings.LeaseDurationInSec.Value) : EventHubProcessorHostParameters.DEFAULT_LEASE_DURATION,
                RenewInterval = eventHubConsumerSettings.RenewIntervalInSec.HasValue ? TimeSpan.FromSeconds(eventHubConsumerSettings.RenewIntervalInSec.Value) : EventHubProcessorHostParameters.DEFAULT_RENEW_INTERVAL,
            };

            return processor;
        }

        public static EventProcessorOptions GetDefaultProcessorHostOptions(DateTime? startEnqueuedTimeUtc, EventHubProcessorHostParameters hostParameters)
        {
            var options = new EventProcessorOptions()
            {
                MaxBatchSize = hostParameters.MaxBatchSize,
                PrefetchCount = hostParameters.MaxBatchSize * 3,
                InvokeProcessorAfterReceiveTimeout = true,
                ReceiveTimeout = TimeSpan.FromMinutes(1), // just to have feedback when nothing moves,
            };

            if (startEnqueuedTimeUtc.HasValue)
            {
                options.InitialOffsetProvider = partition => EventPosition.FromEnqueuedTime(startEnqueuedTimeUtc.Value);
            }

            return options;
        }

        #endregion


        // https://github.com/Azure/azure-sdk-for-net/blob/809f48630e06b7672b4f3475f814cd46bfd97b33/sdk/eventhub/Microsoft.Azure.EventHubs.Processor/src/AzureStorageCheckpointLeaseManager.cs#L18
        public const string METADATA_OWNERNAME = "OWNINGHOST";

        public static async Task HandleMetadataCase(EventHubProcessorHostParameters hostParameters, EventHubConsumerSettings eventHubConsumerSettings, ILogger logger)
        {
            // HANDLE az-copy bug regarding metadata case
            // https://github.com/Azure/azure-storage-azcopy/issues/113#issuecomment-598146034

            var fileProvider = new AzureBlobStorageProvider(
                eventHubConsumerSettings.BlobStorage.GetStorageAccount(),
                GetLeaseContainerName(hostParameters),
                zippedFiles: false
                );

            var markFile = fileProvider.GetFile($"{hostParameters.ConsumerGroupName}.checkpointMetadataOK.lock");
            if (!markFile.Exists().Result)
            {
                logger.LogObject($"{markFile} file doesn't existing.");

                var files = fileProvider.GetFiles(hostParameters.ConsumerGroupName).Cast<AzureBlobStorageProvider.AzureBlobStorageFile>();
                await files.Select(async file =>
                {
                    await file.BlobReference.FetchAttributesAsync().CAF();
                    var meta = file.BlobReference.Metadata;

                    var owner = meta.FirstOrDefault(kv => string.Compare(kv.Key, METADATA_OWNERNAME, StringComparison.InvariantCultureIgnoreCase) == 0);
                    if (owner.Key == null || owner.Key == METADATA_OWNERNAME)
                        return;

                    logger.LogObject($"Correcting {file} file '{owner.Key}' => '{METADATA_OWNERNAME}' case.");

                    var value = owner.Value;
                    meta.Remove(owner.Key);
                    meta[METADATA_OWNERNAME] = value;

                    await file.BlobReference.SetMetadataAsync().CAF();
                }).InfiniteWhenAll(10);

                logger.LogObject($"Writing {markFile}");
                await markFile.WriteTextAsync("ok").CAF();
            }
        }


        #region Monitoring

        internal static async Task<CloudTable> PrepareMonitoringCloudTable(string storageConnectionString, string monitoringTableName)
        {
            var storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            var tableClient = storageAccount.CreateCloudTableClient();
            var table = tableClient.GetTableReference(monitoringTableName);

            tableClient.DefaultRequestOptions.RetryPolicy = StorageHelper.GetDefaultRetryPolicy();

            await table.CreateIfNotExistsAsync().CAF();

            return table;
        }

        internal static async Task SaveCheckPointInformationAsync(CloudTable table, PartitionContext context, EventData last, string hostname, string @namespace)
        {
            TrackingRecorderCheckpointStatus statusEntity;

            var partitionKey = @namespace + "-" + context.EventHubPath + "-" + context.ConsumerGroupName;
            var rowKey = context.PartitionId;

            if (last == null)
            {
                statusEntity = new TrackingRecorderCheckpointStatus()
                {
                    PartitionKey = partitionKey,
                    ConsumerGroupName = context.ConsumerGroupName,
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

                var tableResult = await table.ExecuteAsync(TableOperation.Retrieve<FullTrackingRecorderCheckpointStatus>(partitionKey, rowKey)).CAF();

                long? previousSequenceNumber = null;
                DateTime? previousCheckedUtcDate = null;

                if (tableResult.Result != null)
                {
                    var x = (FullTrackingRecorderCheckpointStatus)tableResult.Result;
                    previousCheckedUtcDate = x.LastCheckedUtcDate;
                    previousSequenceNumber = x.SequenceNumber;
                }

                if (previousCheckedUtcDate.HasValue && previousSequenceNumber.HasValue)
                {
                    messageRate = last.SystemProperties.SequenceNumber - previousSequenceNumber.Value;
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
                    ConsumerGroupName = context.ConsumerGroupName,
                    HostName = hostname,
                    LastEnqueuedUtcDate = last.SystemProperties.EnqueuedTimeUtc,
                    LastCheckedUtcDate = lastCheckedUtcDate,
                    SequenceNumber = last.SystemProperties.SequenceNumber,
                    PreviousSequenceNumber = previousSequenceNumber,
                    PreviousCheckedUtcDate = previousCheckedUtcDate,
                    MessageRateSincePreviousCheck = messageRate,
                    DurationSincePreviousCheck = duration.ToString("c"),
                    LastEventId = eventId
                };
            }


            var insertOperation = TableOperation.InsertOrMerge(statusEntity);
            await table.ExecuteAsync(insertOperation).CAF();
        }

        #endregion

        #region Reactive consuming

        public static IDisposable GetConsumedEventDataCheckpointSubscription(IObservable<EventData> consumedEventDataSubject, Func<EventData> firstSequenceNumberValueGot, Action<EventDataAndSequenceNumber> doOnCheckpoint)
            => GetConsumedEventDataCheckpointSubscription(
                consumedEventDataSubject.Select(e =>
                {
                    e.Dispose(); // to avoid a memory leak;
                    return new EventDataAndSequenceNumber(e, e.SystemProperties.SequenceNumber);
                }),
                () =>
                {
                    var e = firstSequenceNumberValueGot();
                    return new EventDataAndSequenceNumber(e, e.SystemProperties.SequenceNumber);
                },
                doOnCheckpoint
                );


        internal static IDisposable GetConsumedEventDataCheckpointSubscription(IObservable<EventDataAndSequenceNumber> consumedEventDataSubject, Func<EventDataAndSequenceNumber> firstSequenceNumberValueGot, Action<EventDataAndSequenceNumber> doOnCheckpoint)
        {
            var bufferClosingSubject = new Subject<bool>();
            var bufferClosing = bufferClosingSubject.StartWith(true);

            Action nextbuffer = () =>
            {
                try
                { if (!bufferClosingSubject.IsDisposed) bufferClosingSubject.OnNext(true); }
                catch (ObjectDisposedException) { /* When the consumers have been destroyed */ }
            };


            Func<IList<EventDataAndSequenceNumber>, bool> isEmptyBuffer = buffer => buffer.Count == 0 || buffer[0] == null;

            var myScheduler = NewThreadScheduler.Default;

            var subscription = consumedEventDataSubject
                .ObserveOn(myScheduler)
                .Scan(new EventDataScanAggregationSeed(firstSequenceNumberValueGot), (seed, eventData) => seed.Aggregate(eventData))
                .Select(x => x.Checkpoint)
                .DistinctUntilChanged(EventDataIntervalComparer.Instance)
                .Buffer(bufferClosing)

                .ObserveOn(myScheduler)
                .Do(buffer =>
                {
                    if (isEmptyBuffer(buffer))
                    {
                        Task.Delay(SpinwaitPeriod).Wait();
                        nextbuffer();
                    }
                })
                .Where(buffer => !isEmptyBuffer(buffer))
                .Select(buffer => buffer.Last())
                .ObserveOn(myScheduler)
                .Subscribe(e =>
                {
                    try
                    { if (e.EventData != null) doOnCheckpoint(e); }
                    catch (Exception)
                    {
                        // Checkpoint can fail, that life, i'll be retried later or on another instance
                    }
                    finally { nextbuffer(); }
                });

            return new GenericDisposable(
                subscription,
                new GenericDisposable(() => { nextbuffer(); Task.Delay(SpinwaitPeriod).Wait(); }),
                bufferClosingSubject
                );
        }

        internal class EventDataScanAggregationSeed
        {
            private readonly Func<EventDataAndSequenceNumber> _firstSequenceNumberValueGot;
            public EventDataScanAggregationSeed(Func<EventDataAndSequenceNumber> firstSequenceNumberValueGot)
            {
                _firstSequenceNumberValueGot = firstSequenceNumberValueGot;
            }

            public SortedSet<EventDataInterval> ConsumedIntervals { get; private set; } = new SortedSet<EventDataInterval>(EventDataIntervalComparer.Instance);

            public EventDataAndSequenceNumber Checkpoint { get; private set; }

            public EventDataScanAggregationSeed Aggregate(EventDataAndSequenceNumber eventData) { InternalAggregate(eventData); return this; }

            private void InternalAggregate(EventDataAndSequenceNumber eventData)
            {
                if (Checkpoint == null)
                {
                    var firstValue = _firstSequenceNumberValueGot();
                    if (firstValue != null)
                        Checkpoint = new EventDataAndSequenceNumber(null, firstValue.SequenceNumber - 1);
                }

                // not really possible, a late event data arrive here while it's no more attended, and probably already checkpointed
                //if (Checkpoint != null && eventData.SequenceNumber <= Checkpoint.SequenceNumber) return;

                // finding an interval to put the value
                var interval = default(EventDataInterval);
                foreach (var span in ConsumedIntervals)
                {
                    if (eventData.SequenceNumber < span.bottom.SequenceNumber - 1)
                        break;
                    interval = span;
                }

                var shouldCreateInterval = interval == null;
                if (interval != null)
                {
                    if (interval.bottom.SequenceNumber <= eventData.SequenceNumber && interval.top.SequenceNumber >= eventData.SequenceNumber)
                    {
                        return; // nothing  changed
                    }
                    else if (interval.bottom.SequenceNumber == (eventData.SequenceNumber + 1))
                    {
                        interval.bottom = eventData;
                    }
                    else if (interval.top.SequenceNumber == (eventData.SequenceNumber - 1))
                    {
                        interval.top = eventData;
                    }
                    else
                    {
                        shouldCreateInterval = true;
                    }
                }

                if (shouldCreateInterval)
                {
                    interval = new EventDataInterval { bottom = eventData, top = eventData };
                    ConsumedIntervals.Add(interval);
                }

                var previous = default(EventDataInterval);
                var intervalsToRemove = new List<EventDataInterval>();
                foreach (var span in ConsumedIntervals)
                {
                    var shouldRemove = false;

                    if (previous != null && previous.top.SequenceNumber + 1 == span.bottom.SequenceNumber)
                    {
                        previous.top = span.top;
                        shouldRemove = true;
                    }

                    if (Checkpoint != null && Checkpoint.SequenceNumber >= span.top.SequenceNumber)
                    {
                        shouldRemove = true;
                    }
                    else if (Checkpoint != null && Checkpoint.SequenceNumber + 1 >= span.bottom.SequenceNumber)
                    {
                        Checkpoint = span.top;
                        shouldRemove = true;
                    }

                    previous = span;
                    if (shouldRemove)
                        intervalsToRemove.Add(span);
                }
                foreach (var span in intervalsToRemove)
                    ConsumedIntervals.Remove(span);

            }

            public override string ToString()
            {
                return $"cp:{Checkpoint?.SequenceNumber.ToString() ?? "nope"} - intervals : {string.Join(",", ConsumedIntervals.Select(i => $"{i.bottom.SequenceNumber}-{i.top.SequenceNumber}"))}";
            }
        }

        internal class EventDataInterval
        {
            public EventDataAndSequenceNumber bottom;
            public EventDataAndSequenceNumber top;
        }

        public class EventDataAndSequenceNumber
        {
            public EventDataAndSequenceNumber(EventData eventData, long sequenceNumber)
            {
                EventData = eventData;
                SequenceNumber = sequenceNumber;
            }

            public EventData EventData { get; private set; }
            public long SequenceNumber { get; private set; }
        }

        internal class EventDataIntervalComparer : IComparer<EventDataInterval>, IEqualityComparer<EventDataInterval>, IComparer<EventDataAndSequenceNumber>, IEqualityComparer<EventDataAndSequenceNumber>
        {
            public static readonly EventDataIntervalComparer Instance = new EventDataIntervalComparer();

            private EventDataIntervalComparer() { }

            public int Compare(EventDataInterval x, EventDataInterval y) => Compare(x.bottom, y.bottom);
            public bool Equals(EventDataInterval x, EventDataInterval y) => Equals(x.bottom, y.bottom);
            public int GetHashCode(EventDataInterval obj) => GetHashCode(obj.bottom);

            public int Compare(EventDataAndSequenceNumber x, EventDataAndSequenceNumber y)
                => (int)((x?.SequenceNumber ?? DEFAULT_CHECKPOINT_VALUE) - (y?.SequenceNumber ?? DEFAULT_CHECKPOINT_VALUE));

            public bool Equals(EventDataAndSequenceNumber x, EventDataAndSequenceNumber y)
                => (x?.SequenceNumber ?? DEFAULT_CHECKPOINT_VALUE) == (y?.SequenceNumber ?? DEFAULT_CHECKPOINT_VALUE);

            public int GetHashCode(EventDataAndSequenceNumber obj)
                => obj?.SequenceNumber.GetHashCode() ?? 0;
        }

        #endregion
    }
}
