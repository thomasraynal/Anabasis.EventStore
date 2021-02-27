using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using MoreLinq;
using System.Reactive.Linq;
using Anabasis.EventStore.Infrastructure;

namespace Anabasis.EventStore
{
  public class EventStoreRepository<TKey> : IEventStoreRepository<TKey>, IDisposable
  {
    private readonly IEventStoreConnection _eventStoreConnection;
    private readonly IEventTypeProvider<TKey> _eventTypeProvider;
    private readonly IDisposable _cleanup;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly IEventStoreRepositoryConfiguration<TKey> _eventStoreRepositoryConfiguration;

    public bool IsConnected { get; private set; }

    public EventStoreRepository(
        IEventStoreRepositoryConfiguration<TKey> eventStoreRepositoryConfiguration,
        IEventStoreConnection eventStoreConnection,
        IConnectionStatusMonitor connectionMonitor,
        IEventTypeProvider<TKey> eventTypeProvider,
        Microsoft.Extensions.Logging.ILogger logger = null)
    {

      _logger = logger ?? new DummyLogger();

      _eventStoreRepositoryConfiguration = eventStoreRepositoryConfiguration;
      _eventStoreConnection = eventStoreConnection;
      _eventTypeProvider = eventTypeProvider;

      _cleanup = connectionMonitor.OnConnected
            .Subscribe( (Action<bool>)(isConnected =>
            {
              this.IsConnected = isConnected;

            }));
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

    private async Task Save<TEvent>(IEvent<TKey> @event, params KeyValuePair<string, string>[] extraHeaders)
        where TEvent : IEvent<TKey>
    {

      var commitHeaders = CreateCommitHeaders(@event, extraHeaders);

      var eventsToSave = new[] { ToEventData(Guid.NewGuid(), @event, commitHeaders) };

      await SaveEventBatch(@event.GetStreamName(), ExpectedVersion.Any, eventsToSave);

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

    private async Task SaveEventBatch(string streamName, int expectedVersion, EventData[] eventsToSave)
    {
      var eventBatches = GetEventBatches(eventsToSave);

      if (eventBatches.Count == 1)
      {
        await _eventStoreConnection.AppendToStreamAsync(streamName, expectedVersion, eventBatches.Single());
      }
      else
      {
        using var transaction = await _eventStoreConnection.StartTransactionAsync(streamName, expectedVersion);

        foreach (var batch in eventBatches)
        {
          await transaction.WriteAsync(batch);
        }

        await transaction.CommitAsync();
      }

    }

    private IEvent<TKey> DeserializeEvent(RecordedEvent evt)
    {
      var targetType = _eventTypeProvider.GetEventTypeByName(evt.EventType);

      if (null == targetType) throw new InvalidOperationException($"{evt.EventType} cannot be handled");

      return _eventStoreRepositoryConfiguration.Serializer.DeserializeObject(evt.Data, targetType) as IEvent<TKey>;
    }

    private IList<IList<EventData>> GetEventBatches(IEnumerable<EventData> events)
    {
      return events.Batch(_eventStoreRepositoryConfiguration.WritePageSize).Select(x => (IList<EventData>)x.ToList()).ToList();
    }

    protected virtual IDictionary<string, string> GetCommitHeaders(object aggregate)
    {
      var commitId = Guid.NewGuid();

      return new Dictionary<string, string>
            {
                {MetadataKeys.CommitIdHeader, commitId.ToString()},
                {MetadataKeys.AggregateClrTypeHeader, aggregate.GetType().AssemblyQualifiedName},
                {MetadataKeys.UserIdentityHeader, Thread.CurrentPrincipal?.Identity?.Name},
                {MetadataKeys.ServerNameHeader, Environment.MachineName},
                {MetadataKeys.ServerClockHeader, DateTime.UtcNow.ToString("o")}
            };
    }

    private IDictionary<string, string> CreateCommitHeaders(object aggregate, KeyValuePair<string, string>[] extraHeaders)
    {
      var commitHeaders = GetCommitHeaders(aggregate);

      foreach (var extraHeader in extraHeaders)
      {
        commitHeaders[extraHeader.Key] = extraHeader.Value;
      }

      return commitHeaders;
    }

    private EventData ToEventData<TEvent>(Guid eventId, TEvent @event, IDictionary<string, string> headers)
        where TEvent : IEvent<TKey>
    {

      var data = _eventStoreRepositoryConfiguration.Serializer.SerializeObject(@event);

      var eventHeaders = new Dictionary<string, string>(headers)
            {
                {MetadataKeys.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName}
            };

      var metadata = _eventStoreRepositoryConfiguration.Serializer.SerializeObject(eventHeaders);
      var typeName = @event.GetType().FullName;

      return new EventData(eventId, typeName, true, data, metadata);
    }

    public async Task Emit<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders)
        where TEvent : IEvent<TKey>
    {
      await Save<TEvent>(@event, extraHeaders);
    }

    public async Task Apply<TEntity, TEvent>(TEntity aggregate, TEvent ev, params KeyValuePair<string, string>[] extraHeaders)
        where TEntity : IAggregate<TKey>
        where TEvent : IEvent<TKey>, IMutable<TKey, TEntity>
    {

      aggregate.ApplyEvent(ev);

      await Save(aggregate, extraHeaders);

    }

    public void Dispose()
    {
      _cleanup.Dispose();
    }
  }
}
