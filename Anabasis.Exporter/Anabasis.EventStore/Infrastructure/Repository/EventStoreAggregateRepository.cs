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

    private async Task Save(IAggregate<TKey> aggregate, params KeyValuePair<string, string>[] extraHeaders)
    {

      var streamName = aggregate.GetStreamName();

      var afterApplyAggregateVersion = aggregate.Version;

      var commitHeaders = CreateCommitHeaders(aggregate, extraHeaders);

      var eventsToSave = aggregate.PendingEvents.Select(ev => ToEventData(Guid.NewGuid(), ev, commitHeaders)).ToArray();

      await SaveEventBatch(streamName, afterApplyAggregateVersion, eventsToSave);

      aggregate.ClearPendingEvents();
    }

    public async Task Apply<TEntity, TEvent>(TEntity aggregate, TEvent ev, params KeyValuePair<string, string>[] extraHeaders)
    where TEntity : IAggregate<TKey>
    where TEvent : IEntityEvent<TKey>, IMutable<TKey, TEntity>
    {

      aggregate.ApplyEvent(ev);

      await Save(aggregate, extraHeaders);

    }

    private IEntityEvent<TKey> DeserializeEvent(RecordedEvent recordedEvent)
    {
      var targetType = _eventTypeProvider.GetEventTypeByName(recordedEvent.EventType);

      if (null == targetType) throw new InvalidOperationException($"{recordedEvent.EventType} cannot be handled");

      return _eventStoreRepositoryConfiguration.Serializer.DeserializeObject(recordedEvent.Data, targetType) as IEntityEvent<TKey>;
    }
  }
}
