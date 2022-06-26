using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using BeezUP2.Framework.Application;
using BeezUP2.Framework.Configuration;
using BeezUP2.Framework.Insights;
using BeezUP2.Framework.Queuing;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.WindowsAzure.Storage.Table;
using OpenTracing;

namespace BeezUP2.Framework.EventHubs
{
    public class ScalingEventHubProcessor2 : IEventProcessor
    {
        readonly TimeSpan _delayBetweenCheckpoints = TimeSpan.FromMinutes(3);

        readonly EventHubProcessorHostParameters _parameters;
        readonly string _monitoringTableName;
        readonly Func<IObservable<IConsumable<EventData>>, IDisposable> _subscribeEventDatas;
        readonly ILogger _logger;
        readonly ITracer _tracer;
        readonly int _maxInProgressEventdataCount;
        readonly IBeezUPAppKillButton _killButton;

        Subject<IConsumable<EventData>> _eventDataSubject;
        Subject<EventData> _consumedEventDataSubject;
        IDisposable _disposables;

        CloudTable _table;
        EventData _firstEventDataGot = null;

        public ScalingEventHubProcessor2(
            EventHubProcessorHostParameters parameters,
            string monitoringTableName,
            Func<IObservable<IConsumable<EventData>>, IDisposable> subscribeEventDatas,
            BeezUPAppContext appContext,
            int maxInProgressEventdataCount
            )
        {
            _parameters = parameters;
            _monitoringTableName = monitoringTableName;
            _subscribeEventDatas = subscribeEventDatas;
            _logger = appContext.Logger;
            _tracer = appContext.Tracer;
            _maxInProgressEventdataCount = maxInProgressEventdataCount;
            _killButton = appContext;
        }

        int _inProgressEventdataCount;

        void Log(PartitionContext context, string message)
        {
            var id = $"{nameof(ScalingEventHubProcessor2)} {_parameters.Connection.Namespace}/{_parameters.Connection.HubName}:{_parameters.ConsumerGroupName} - Partition {context.PartitionId}";
            _logger.LogObject($"{id} ::: {message}");
        }

        private DateTime _latestCheckpoint = DateTime.UtcNow;

        public async Task OpenAsync(PartitionContext context)
        {
            Log(context, "initialized");

            _table = await EventHubsHelper.PrepareMonitoringCloudTable(_parameters.EventHubConsumerSettings.TableStorage.GetStorageConnectionString(), _monitoringTableName);

            _eventDataSubject = new Subject<IConsumable<EventData>>();
            _consumedEventDataSubject = new Subject<EventData>();
            _disposables =
                new CompositeDisposable(
                    _subscribeEventDatas(_eventDataSubject),
                    EventHubsHelper.GetConsumedEventDataCheckpointSubscription(
                        _consumedEventDataSubject,
                        () => _firstEventDataGot,
                        checkpoint =>
                        {
                            if (_closeNow)
                                return;

                            var now = DateTime.UtcNow;
                            if (now - _latestCheckpoint > _delayBetweenCheckpoints)
                            {
                                _latestCheckpoint = now;

                                try
                                {
                                    Task.WhenAll(
                                        context.CheckpointAsync(checkpoint.EventData),
                                        EventHubsHelper.SaveCheckPointInformationAsync(_table, context, checkpoint.EventData, _parameters.EventProcessorHostName, _parameters.Connection.Namespace)
                                        ).Wait();
                                }
                                catch (LeaseLostException)
                                {
                                    // ignore
                                }
                                catch (Exception ex)
                                {
                                    if (_closeNow)
                                        return;
                                    _logger.LogException(ex);
                                    _killButton.KillBeezUPApp("Error while checkpointing EventHubs.", ex);
                                }
                            }
                        }),
                    _consumedEventDataSubject
                    );
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            if (_firstEventDataGot == null)
                _firstEventDataGot = messages.FirstOrDefault();
            var ct = context.CancellationToken;

            foreach (var ed in messages)
            {
                Interlocked.Increment(ref _inProgressEventdataCount);

                while (_inProgressEventdataCount >= _maxInProgressEventdataCount && !ct.IsCancellationRequested)
                    await Task.Delay(10, ct);

                if (_closeNow)
                    break;

                if (ct.IsCancellationRequested)
                    return;

                var spanContext = _tracer.ExtractContext(ed);

                try
                {
                    _eventDataSubject.OnNext(
                        new EventDataConsumable(
                            ed,
                            e =>
                            {
                                if (_closeNow)
                                    return;

                                try
                                {
                                    Interlocked.Decrement(ref _inProgressEventdataCount);
                                    _consumedEventDataSubject.OnNext(e);
                                }
                                catch (Exception ex)
                                {
                                    if (_closeNow)
                                        return;
                                    Log(context, ex.Message);
                                    //_killButton.KillBeezUPApp("Error while handling EventHubs events.", ex);
                                }
                            },
                            spanContext
                            ));
                }
                catch (ObjectDisposedException ex)
                {
                    if (_closeNow)
                        return;
                    Log(context, ex.Message);
                }
            }
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Log(context, $"error -> {error.Message}");
            _logger.LogException(error);
            // _consumedEventDataSubject?.OnError(error); // this line lead to the host process crash !
            return Task.CompletedTask;
        }

        bool _closeNow = false;
        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            _closeNow = true;
            Log(context, $"close (reason : {reason})");
            _disposables.Dispose();
            // no auto checkpoint here
            return Task.CompletedTask;
        }
    }
}
