using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using System.Reactive.Linq;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using Anabasis.Common;
using Anabasis.Common.Utilities;

namespace Anabasis.EventStore.Repository
{
    public class EventStoreRepository : IEventStoreRepository, IDisposable
    {
        protected readonly IEventStoreConnection _eventStoreConnection;
        protected readonly IEventStoreRepositoryConfiguration _eventStoreRepositoryConfiguration;

        private readonly IDisposable _cleanup;
        protected  ILogger<EventStoreRepository> Logger { get; }

        public bool IsConnected { get; private set; }

        public string Id { get; }

        public EventStoreRepository(
            IEventStoreRepositoryConfiguration eventStoreRepositoryConfiguration,
            IEventStoreConnection eventStoreConnection,
            IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
            ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory?.CreateLogger<EventStoreRepository>();

            Id = $"{GetType()}-{Guid.NewGuid()}";

            _eventStoreRepositoryConfiguration = eventStoreRepositoryConfiguration;
            _eventStoreConnection = eventStoreConnection;

            _cleanup = connectionMonitor.OnConnected
                  .Subscribe(isConnected =>
                  {
                      IsConnected = isConnected;

                  });
        }

        private async Task Save(IEnumerable<IEvent> events, params KeyValuePair<string, string>[] extraHeaders)
        {

            foreach (var eventBatch in events.GroupBy(ev => ev.EntityId))
            {
                var eventsToSave = eventBatch.Select(@event =>
                {
                    var commitHeaders = CreateCommitHeaders(null, @event, extraHeaders);

                    Logger?.LogDebug($"{Id} => Emitting event: {@event.EventId} {@event.EntityId} {@event.GetType()}");

                    return ToEventData(@event.EventId, @event, commitHeaders);

                });

                await SaveEventBatch(eventBatch.Key, ExpectedVersion.Any, eventsToSave.ToArray());
            }

        }

        protected async Task SaveEventBatch(string streamId, int expectedVersion, IEnumerable<EventData> eventsToSave)
        {
            WriteResult writeResult;

            var eventBatches = GetEventBatches(eventsToSave);

            foreach (var batch in eventBatches)
            {

                if (batch.Length == 1)
                {
                    writeResult = await _eventStoreConnection.AppendToStreamAsync(streamId, expectedVersion, batch.Single());
                }
                else
                {
                    using var transaction = await _eventStoreConnection.StartTransactionAsync(streamId, expectedVersion);

                    await transaction.WriteAsync(batch);

                    writeResult = await transaction.CommitAsync();
                }
            }
        }

        protected IEnumerable<EventData[]> GetEventBatches(IEnumerable<EventData> events)
        {
            return events.Batch(_eventStoreRepositoryConfiguration.WritePageSize).Select(batch => batch.ToArray());
        }

        protected virtual IDictionary<string, string> GetCommitHeaders(object aggregate, object @event)
        {
            var commitId = Guid.NewGuid();

            var commitHeaders = new Dictionary<string, string>
            {
                {MetadataKeys.CommitIdHeader, commitId.ToString()},
                {MetadataKeys.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName},
                {MetadataKeys.UserIdentityHeader, Thread.CurrentPrincipal?.Identity?.Name},
                {MetadataKeys.ServerNameHeader, Environment.MachineName},
                {MetadataKeys.ServerClockHeader, DateTime.UtcNow.ToString("o")}
            };

            if (null != aggregate)
            {
                commitHeaders.Add(MetadataKeys.AggregateClrTypeHeader, aggregate.GetType().AssemblyQualifiedName);
            }

            return commitHeaders;
        }

        protected IDictionary<string, string> CreateCommitHeaders(object aggregate, object @event, KeyValuePair<string, string>[] extraHeaders)
        {
            var commitHeaders = GetCommitHeaders(aggregate, @event);

            foreach (var extraHeader in extraHeaders)
            {
                commitHeaders[extraHeader.Key] = extraHeader.Value;
            }

            return commitHeaders;
        }

        protected EventData ToEventData(Guid eventId, object @event, IDictionary<string, string> headers)
        {

            var data = _eventStoreRepositoryConfiguration.Serializer.SerializeObject(@event);

            if (!headers.ContainsKey(MetadataKeys.EventClrTypeHeader)){

                headers.Add(MetadataKeys.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName);
            }

            var metadata = _eventStoreRepositoryConfiguration.Serializer.SerializeObject(headers);
            var typeName = @event.GetType().FullName;

            return new EventData(eventId, typeName, true, data, metadata);
        }


        public async Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders)
            where TEvent : IHaveEntityId
        {
            Logger?.LogDebug($"{Id} => Emitting event: {@event.EntityId} {@event.GetType()}");

            var commitHeaders = CreateCommitHeaders(null, @event, extraHeaders);

            var eventsToSave = new[] { ToEventData(Guid.NewGuid(), @event, commitHeaders) };

            await SaveEventBatch(@event.EntityId, ExpectedVersion.Any, eventsToSave);
        }

        public async Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders)
        {
            await Save(new[] { @event }, extraHeaders);
        }

        public async Task Emit(IEnumerable<IEvent> events, params KeyValuePair<string, string>[] extraHeaders)
        {
            await Save(events, extraHeaders);
        }

        public void Dispose()
        {
            _cleanup.Dispose();
        }
    }
}
