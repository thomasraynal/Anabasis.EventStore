using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using Anabasis.Common;

namespace Anabasis.EventStore.Repository
{
    public class EventStoreAggregateRepository : EventStoreRepository, IEventStoreAggregateRepository
    {
        public EventStoreAggregateRepository(IEventStoreRepositoryConfiguration eventStoreRepositoryConfiguration,
          IEventStoreConnection eventStoreConnection,
          IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
          ILoggerFactory loggerFactory) : base(eventStoreRepositoryConfiguration, eventStoreConnection, connectionMonitor, loggerFactory)
        {
        }

        public async Task<TAggregate> GetAggregateByStreamId<TAggregate>(string streamId, long? fromVersion, IEventTypeProvider eventTypeProvider, bool loadEvents = false)
            where TAggregate : class, IAggregate, new()
        {
            if (!IsConnected) throw new InvalidOperationException("Client is not connected to EventStore");

            var aggregate = new TAggregate();

            aggregate.SetEntityId(streamId);

            var eventNumber = fromVersion ?? 0L;

            StreamEventsSlice currentSlice;

            do
            {
                currentSlice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamId, eventNumber, _eventStoreRepositoryConfiguration.ReadPageSize, false, _eventStoreRepositoryConfiguration.UserCredentials);

                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                {
                    throw new InvalidOperationException($"Unable to find stream {streamId}");
                }

                if (currentSlice.Status == SliceReadStatus.StreamDeleted)
                {
                    throw new InvalidOperationException($"Stream {streamId} was deleted");
                }

                eventNumber = currentSlice.NextEventNumber;

                foreach (var resolvedEvent in currentSlice.Events)
                {
                    var @event = DeserializeEvent<TAggregate>(resolvedEvent.Event, eventTypeProvider, false);

                    if (null == @event) continue;

                    aggregate.ApplyEvent(@event, false, loadEvents);
                }

            } while (!currentSlice.IsEndOfStream);

            return aggregate;
        }


        public async Task Apply<TAggregate, TEvent>(TAggregate aggregate, TEvent @event, params KeyValuePair<string, string>[] extraHeaders)
            where TAggregate : class, IAggregate
            where TEvent : IAggregateEvent<TAggregate>
        {

            Logger?.LogDebug($"{Id} => Applying event: {@event.EntityId} {@event.GetType()}");

            aggregate.ApplyEvent(@event);

            var afterApplyAggregateVersion = aggregate.Version;

            var commitHeaders = CreateCommitHeaders(aggregate, @event, extraHeaders);

            var eventsToSave = aggregate.PendingEvents.Select(ev => ToEventData(Guid.NewGuid(), ev, commitHeaders)).ToArray();

            await SaveEventBatch(aggregate.EntityId, afterApplyAggregateVersion, eventsToSave);

            aggregate.ClearPendingEvents();

        }

        private IAggregateEvent<TAggregate>? DeserializeEvent<TAggregate>(RecordedEvent recordedEvent, IEventTypeProvider eventTypeProvider, bool throwIfNotHandled = true)
            where TAggregate : class, IAggregate
        {
            var targetType = eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            if (null == targetType && throwIfNotHandled) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");
            if (null == targetType) return null;

            return _eventStoreRepositoryConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IAggregateEvent<TAggregate>;
        }
    }
}
