﻿using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Stream;
using EventStore.ClientAPI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public class EventStoreBus : IEventStoreBus
    {

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

            ConnectionStatusMonitor = _connectionStatusMonitor = connectionStatusMonitor;

            _eventStoreRepository = eventStoreRepository;
            _cleanUp = new CompositeDisposable();
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger(typeof(EventStoreBus));
        }

        public string BusId { get; }

        public IConnectionStatusMonitor ConnectionStatusMonitor { get; }

        //use this as hc => https://developers.eventstore.com/server/v20.10/diagnostics.html#statistics
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

        public void Dispose()
        {
            _cleanUp.Dispose();
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

        public IDisposable SubscribeToPersistentSubscriptionStream(string streamId,
            string groupId,
            Action<IMessage> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<PersistentSubscriptionStreamConfiguration>? getPersistentSubscriptionEventStoreStreamConfiguration = null)
        {
            var persistentEventStoreStreamConfiguration = new PersistentSubscriptionStreamConfiguration(streamId, groupId);

            getPersistentSubscriptionEventStoreStreamConfiguration?.Invoke(persistentEventStoreStreamConfiguration);

            var persistentSubscriptionEventStoreStream = new PersistentSubscriptionEventStoreStream(
                  _connectionStatusMonitor,
                  persistentEventStoreStreamConfiguration,
                  eventTypeProvider,
                  _loggerFactory);


            return SubscribeToEventStream(persistentSubscriptionEventStoreStream, onMessageReceived);

        }

        private IDisposable SubscribeToEventStream(IEventStoreStream eventStoreStream, Action<IMessage> onMessageReceived)
        {
            eventStoreStream.Connect();

            _logger?.LogDebug($"{BusId} => Subscribing to {eventStoreStream.Id}");

            var onEventReceivedDisposable = eventStoreStream.OnMessage().Subscribe(message =>
            {
                onMessageReceived(message);
            });

            _cleanUp.Add(eventStoreStream);
            _cleanUp.Add(onEventReceivedDisposable);

            return new CompositeDisposable(onEventReceivedDisposable, eventStoreStream);
        }

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

        public IDisposable SubscribeToManyStreams(string[] streamIds,
            Action<IMessage> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneStreamConfiguration>? getSubscribeToOneOrManyStreamsConfiguration = null)
        {
            var subscribeToOneStreamEventStoreStreams = streamIds.Select(streamId =>
            {
                _logger?.LogDebug($"{BusId} => Subscribing to {streamId}");

                var subscribeToOneStreamEventStoreStream = CreateSubscribeToOneStream(streamId, null, eventTypeProvider, getSubscribeToOneOrManyStreamsConfiguration);

                subscribeToOneStreamEventStoreStream.Connect();

                var onEventReceivedDisposable = subscribeToOneStreamEventStoreStream.OnMessage().Subscribe(message =>
                {
                    onMessageReceived(message);
                });

                _cleanUp.Add(subscribeToOneStreamEventStoreStream);
                _cleanUp.Add(onEventReceivedDisposable);

                return new CompositeDisposable(onEventReceivedDisposable, subscribeToOneStreamEventStoreStream);

            }).ToArray();

            return new CompositeDisposable(subscribeToOneStreamEventStoreStreams);

        }

        private SubscribeToOneStreamEventStoreStream CreateSubscribeToOneStream(string streamId,
            long? streamPosition, 
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneStreamConfiguration>? getSubscribeToOneStreamConfiguration = null)
        {
            var subscribeToOneStreamConfiguration = new SubscribeToOneStreamConfiguration(streamId, streamPosition);

            getSubscribeToOneStreamConfiguration?.Invoke(subscribeToOneStreamConfiguration);

            var subscribeToOneStreamEventStoreStream = new SubscribeToOneStreamEventStoreStream(
                _connectionStatusMonitor,
                subscribeToOneStreamConfiguration,
                eventTypeProvider,
                _loggerFactory);

            return subscribeToOneStreamEventStoreStream;
        }

        public IDisposable SubscribeToOneStream(string streamId,
            int streamPosition,
            Action<IMessage> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneStreamConfiguration>? getSubscribeToOneStreamConfiguration = null)
        {

            var subscribeToOneStreamConfiguration = new SubscribeToOneStreamConfiguration(streamId, streamPosition);

            getSubscribeToOneStreamConfiguration?.Invoke(subscribeToOneStreamConfiguration);

            var subscribeToOneStreamEventStoreStream = CreateSubscribeToOneStream(streamId, streamPosition, eventTypeProvider, getSubscribeToOneStreamConfiguration);

            _logger?.LogDebug($"{BusId} => Subscribing to {subscribeToOneStreamEventStoreStream.Id}");

            subscribeToOneStreamEventStoreStream.Connect();

            var onEventReceivedDisposable = subscribeToOneStreamEventStoreStream.OnMessage().Subscribe(message =>
            {
                onMessageReceived(message);
            });

            _cleanUp.Add(subscribeToOneStreamEventStoreStream);
            _cleanUp.Add(onEventReceivedDisposable);

            return new CompositeDisposable(onEventReceivedDisposable, subscribeToOneStreamEventStoreStream);
        }
    }
}
