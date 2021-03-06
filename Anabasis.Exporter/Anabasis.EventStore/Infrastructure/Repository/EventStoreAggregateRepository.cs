using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using System.Reactive.Linq;

namespace Anabasis.EventStore.Infrastructure.Repository
{
  public class EventStoreAggregateRepository<TKey> : EventStoreRepository, IEventStoreAggregateRepository<TKey>
  {
    public EventStoreAggregateRepository(IEventStoreRepositoryConfiguration eventStoreRepositoryConfiguration,
      IEventStoreConnection eventStoreConnection, IConnectionStatusMonitor connectionMonitor,
      IEventTypeProvider eventTypeProvider,
      Microsoft.Extensions.Logging.ILogger logger = null) : base(eventStoreRepositoryConfiguration, eventStoreConnection, connectionMonitor, eventTypeProvider, logger)
    {
    }

    public async Task<TAggregate> GetById<TAggregate>(TKey id, bool loadEvents = false) where TAggregate : IAggregate<TKey>, new()
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
          var @event = DeserializeEvent(resolvedEvent.Event);

          aggregate.ApplyEvent(@event, false, loadEvents);
        }

      } while (!currentSlice.IsEndOfStream);

      return aggregate;
    }
    public async Task Apply<TEntity, TEvent>(TEntity aggregate, TEvent ev, params KeyValuePair<string, string>[] extraHeaders)
    where TEntity : IAggregate<TKey>
    where TEvent : IEntityEvent<TKey>, IMutable<TKey, TEntity>
    {

      aggregate.ApplyEvent(ev);

      var afterApplyAggregateVersion = aggregate.Version;

      var commitHeaders = CreateCommitHeaders(aggregate, extraHeaders);

      var eventsToSave = aggregate.PendingEvents.Select(ev => ToEventData(Guid.NewGuid(), ev, commitHeaders)).ToArray();

      await SaveEventBatch(aggregate.StreamId, afterApplyAggregateVersion, eventsToSave);

      aggregate.ClearPendingEvents();

    }

    public async Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders)
    where TEvent : IEntityEvent<TKey>
    {
      var commitHeaders = CreateCommitHeaders(@event, extraHeaders);

      var eventsToSave = new[] { ToEventData(Guid.NewGuid(), @event, commitHeaders) };

      await SaveEventBatch(@event.StreamId, ExpectedVersion.Any, eventsToSave);
    }


    private IEntityEvent<TKey> DeserializeEvent(RecordedEvent recordedEvent)
    {
      var targetType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

      if (null == targetType) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");

      return _eventStoreRepositoryConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IEntityEvent<TKey>;
    }
  }
}
