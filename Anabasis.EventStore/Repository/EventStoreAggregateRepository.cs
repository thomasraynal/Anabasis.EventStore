using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using System.Reactive.Linq;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Connection;
using Microsoft.Extensions.Logging;
using Anabasis.Common;

namespace Anabasis.EventStore.Repository
{
    public class EventStoreAggregateRepository : EventStoreRepository, IEventStoreAggregateRepository
    {
        public EventStoreAggregateRepository(IEventStoreRepositoryConfiguration eventStoreRepositoryConfiguration,
          IEventStoreConnection eventStoreConnection,
          IConnectionStatusMonitor connectionMonitor,
          ILoggerFactory loggerFactory) : base(eventStoreRepositoryConfiguration, eventStoreConnection, connectionMonitor, loggerFactory)
        {
        }

        public async Task<TAggregate> GetById<TAggregate>(string id, IEventTypeProvider eventTypeProvider, bool loadEvents = false) where TAggregate : IAggregate, new()
        {
            if (!IsConnected) throw new InvalidOperationException("Client is not connected to EventStore");

            var aggregate = new TAggregate();

            var streamName = $"{id}";

            var eventNumber = 0L;

            StreamEventsSlice currentSlice;

            do
            {
                currentSlice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, eventNumber, _eventStoreRepositoryConfiguration.ReadPageSize, false, _eventStoreRepositoryConfiguration.UserCredentials);

                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                {
                    return default;
                }

                if (currentSlice.Status == SliceReadStatus.StreamDeleted)
                {
                    return default;
                }

                eventNumber = currentSlice.NextEventNumber;

                foreach (var resolvedEvent in currentSlice.Events)
                {
                    var @event = DeserializeEvent(resolvedEvent.Event, eventTypeProvider, false);

                    if (null == @event) continue;

                    aggregate.ApplyEvent(@event, false, loadEvents);
                }

            } while (!currentSlice.IsEndOfStream);

            return aggregate;
        }


        public async Task Apply<TEntity, TEvent>(TEntity aggregate, TEvent @event, params KeyValuePair<string, string>[] extraHeaders)
            where TEntity : IAggregate
            where TEvent : IEntity, IMutation< TEntity>
        {

            Logger?.LogDebug($"{Id} => Applying event: {@event.EntityId} {@event.GetType()}");

            aggregate.ApplyEvent(@event);

            var afterApplyAggregateVersion = aggregate.Version;

            var commitHeaders = CreateCommitHeaders(aggregate, extraHeaders);

            var eventsToSave = aggregate.PendingEvents.Select(ev => ToEventData(Guid.NewGuid(), ev, commitHeaders)).ToArray();

            await SaveEventBatch(aggregate.EntityId, afterApplyAggregateVersion, eventsToSave);

            aggregate.ClearPendingEvents();

        }


        private IEntity DeserializeEvent(RecordedEvent recordedEvent, IEventTypeProvider eventTypeProvider, bool throwIfNotHandled = true)
        {
            var targetType = eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

            if (null == targetType && throwIfNotHandled) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");
            if (null == targetType) return null;

            return _eventStoreRepositoryConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IEntity;
        }
    }
}
