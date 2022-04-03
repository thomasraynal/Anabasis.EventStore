using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Stream;
using EventStore.ClientAPI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public class EventStoreBus : IEventStoreBus
    {
        private readonly Dictionary<Guid, TaskCompletionSource<ICommandResponse>> _pendingCommands;
        private readonly ILoggerFactory? _loggerFactory;
        private readonly Microsoft.Extensions.Logging.ILogger? _logger;
        private readonly IEventStoreRepository _eventStoreRepository;
        private readonly CompositeDisposable _cleanUp;
        private readonly IConnectionStatusMonitor<IEventStoreConnection> _connectionStatusMonitor;

        public EventStoreBus(IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor,
            IEventStoreRepository eventStoreRepository,
            ILoggerFactory? loggerFactory = null)
        {
            BusId = $"{nameof(EventStoreBus)}_{Guid.NewGuid()}";

            _connectionStatusMonitor = connectionStatusMonitor;
            _eventStoreRepository = eventStoreRepository;
            _cleanUp = new CompositeDisposable();
            _pendingCommands = new Dictionary<Guid, TaskCompletionSource<ICommandResponse>>();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger(typeof(EventStoreBus));
        }

        public string BusId { get; }

        public bool IsInitialized => true;

        public IConnectionStatusMonitor ConnectionStatusMonitor => _connectionStatusMonitor;

        public async Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            if (ConnectionStatusMonitor.IsConnected) return;

            var waitUntilMax = DateTime.UtcNow.Add(null == timeout ? Timeout.InfiniteTimeSpan : timeout.Value);

            while (!ConnectionStatusMonitor.IsConnected && DateTime.UtcNow > waitUntilMax)
            {
                await Task.Delay(100);
            }

            if (!ConnectionStatusMonitor.IsConnected) throw new InvalidOperationException("Unable to connect");
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var isConnected = ConnectionStatusMonitor.IsConnected;

            var healthCheckResult = HealthCheckResult.Healthy();

            if (!isConnected)
            {
#nullable disable

                var data = new Dictionary<string, object>()
                {
                    {"EventStore connection is down",(ConnectionStatusMonitor as EventStoreConnectionStatusMonitor).Connection.ConnectionName}
                };
#nullable enable

                healthCheckResult = HealthCheckResult.Unhealthy(data: data);
            }

            return Task.FromResult(healthCheckResult);
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public void SubscribeToEventStream(IEventStoreStream eventStoreStream, Action<IMessage, TimeSpan?> onMessageReceived, bool closeSubscriptionOnDispose = false)
        {
            eventStoreStream.Connect();

            _logger?.LogDebug($"{BusId} => Subscribing to {eventStoreStream.Id}");

            var onEventReceivedDisposable = eventStoreStream.OnMessage().Subscribe(message =>
            {

                if (message.Content is ICommandResponse)
                {


#nullable disable

                    var commandResponse = message.Content as ICommandResponse;

                    if (_pendingCommands.ContainsKey(commandResponse.CommandId))
                    {

                        var task = _pendingCommands[commandResponse.CommandId];

                        if (!task.Task.IsCompleted)
                        {
                            task.SetResult(commandResponse);
                        }

                        _pendingCommands.Remove(commandResponse.EventId, out _);
                    }

#nullable enable

                }
                else
                {
                    onMessageReceived(message, null);
                }
            });

            if (closeSubscriptionOnDispose)
            {
                _cleanUp.Add(eventStoreStream);
            }

            _cleanUp.Add(onEventReceivedDisposable);
        }

        public async Task EmitEventStore<TEvent>(TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent
        {

            if (!_eventStoreRepository.IsConnected)
            {
                await WaitUntilConnected(timeout);
            }

            _logger?.LogDebug($"{BusId} => Emitting {@event.EntityId} - {@event.GetType()}");

            await _eventStoreRepository.Emit(@event, extraHeaders);
        }

        public async Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {
            _logger?.LogDebug($"{BusId} => Sending command {command.EntityId} - {command.GetType()}");

            var taskSource = new TaskCompletionSource<ICommandResponse>();

            var cancellationTokenSource = null != timeout ? new CancellationTokenSource(timeout.Value) : new CancellationTokenSource();

            cancellationTokenSource.Token.Register(() => taskSource.TrySetCanceled(), false);

            _pendingCommands[command.EventId] = taskSource;

            await _eventStoreRepository.Emit(command);

            return await taskSource.Task.ContinueWith(task =>
            {
                if (task.IsCompletedSuccessfully) return (TCommandResult)task.Result;
                if (task.IsCanceled) throw new TimeoutException($"Command {command.EntityId} timeout");
                if (task.IsFaulted && null != task.Exception) ExceptionDispatchInfo.Capture(task.Exception).Throw();

                throw new Exception($"Unable to process CommandResponse - Status => {task.Status}");

            }, cancellationTokenSource.Token);

        }

        public SubscribeFromEndToAllEventStoreStream SubscribeFromEndToAllStreams(
            Action<IMessage, TimeSpan?> onMessageReceived, 
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeFromEndEventStoreStreamConfiguration>? getSubscribeFromEndEventStoreStreamConfiguration = null)
        {

            var subscribeFromEndEventStoreStreamConfiguration = new SubscribeFromEndEventStoreStreamConfiguration();

            getSubscribeFromEndEventStoreStreamConfiguration?.Invoke(subscribeFromEndEventStoreStreamConfiguration);

            var subscribeFromEndEventStoreStream = new SubscribeFromEndToAllEventStoreStream(
              _connectionStatusMonitor,
              subscribeFromEndEventStoreStreamConfiguration,
              eventTypeProvider, _loggerFactory);

            SubscribeToEventStream(subscribeFromEndEventStoreStream, onMessageReceived, true);

            return subscribeFromEndEventStoreStream;
        }

        public PersistentSubscriptionEventStoreStream SubscribeToPersistentSubscriptionStream(
            string streamId,
            string groupId,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<PersistentSubscriptionEventStoreStreamConfiguration>? getPersistentSubscriptionEventStoreStreamConfiguration = null)
        {

            var persistentEventStoreStreamConfiguration = new PersistentSubscriptionEventStoreStreamConfiguration(streamId, groupId);

            getPersistentSubscriptionEventStoreStreamConfiguration?.Invoke(persistentEventStoreStreamConfiguration);

            var persistentSubscriptionEventStoreStream = new PersistentSubscriptionEventStoreStream(
              _connectionStatusMonitor,
              persistentEventStoreStreamConfiguration,
              eventTypeProvider,
              _loggerFactory);

            SubscribeToEventStream(persistentSubscriptionEventStoreStream, onMessageReceived, true);

            return persistentSubscriptionEventStoreStream;
        }

        public SubscribeFromStartOrLaterToOneStreamEventStoreStream SubscribeFromStartToOneStream(
            string streamId,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration>? getSubscribeFromEndToOneStreamEventStoreStreamConfiguration = null)
        {

            var subscribeFromEndToOneStreamEventStoreStreamConfiguration = new SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration(streamId);

            getSubscribeFromEndToOneStreamEventStoreStreamConfiguration?.Invoke(subscribeFromEndToOneStreamEventStoreStreamConfiguration);

            var subscribeFromEndToOneStreamEventStoreStream = new SubscribeFromStartOrLaterToOneStreamEventStoreStream(
              _connectionStatusMonitor,
              subscribeFromEndToOneStreamEventStoreStreamConfiguration,
              eventTypeProvider, _loggerFactory);


            SubscribeToEventStream(subscribeFromEndToOneStreamEventStoreStream, onMessageReceived, true);

            return subscribeFromEndToOneStreamEventStoreStream;

        }

        public SubscribeFromEndToOneStreamEventStoreStream SubscribeFromEndToOneStream(
            string streamId,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration>? getSubscribeFromEndToOneStreamEventStoreStreamConfiguration = null)
        {

            var subscribeFromEndToOneStreamEventStoreStreamConfiguration = new SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration(streamId);

            getSubscribeFromEndToOneStreamEventStoreStreamConfiguration?.Invoke(subscribeFromEndToOneStreamEventStoreStreamConfiguration);

            var subscribeFromEndToOneStreamEventStoreStream = new SubscribeFromEndToOneStreamEventStoreStream(
              _connectionStatusMonitor,
              subscribeFromEndToOneStreamEventStoreStreamConfiguration,
              eventTypeProvider, _loggerFactory);


            SubscribeToEventStream(subscribeFromEndToOneStreamEventStoreStream, onMessageReceived, true);

            return subscribeFromEndToOneStreamEventStoreStream;

        }

        public void Dispose()
        {
            _cleanUp.Dispose();
        }


    }
}
