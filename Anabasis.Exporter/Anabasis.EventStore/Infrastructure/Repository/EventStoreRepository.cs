using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using MoreLinq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Anabasis.EventStore.Infrastructure;

namespace Anabasis.EventStore
{
  public class EventStoreRepository<TKey> : IEventStoreRepository<TKey>, IDisposable
  {
    private readonly IEventStoreConnection _eventStoreConnection;
    private readonly IEventTypeProvider<TKey> _eventTypeProvider;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private readonly IEventStoreRepositoryConfiguration<TKey> _configuration;

    private readonly IScheduler _eventLoopScheduler = new EventLoopScheduler();

    private bool _isConnected;
    private readonly IDisposable _cleanup;

    public EventStoreRepository(
        IEventStoreRepositoryConfiguration<TKey> configuration,
        IEventStoreConnection eventStoreConnection,
        IConnectionStatusMonitor connectionMonitor,
        IEventTypeProvider<TKey> eventTypeProvider,
        Microsoft.Extensions.Logging.ILogger logger = null)
    {

      _logger = logger ?? new DummyLogger();

      _configuration = configuration;
      _eventStoreConnection = eventStoreConnection;
      _eventTypeProvider = eventTypeProvider;

      _cleanup = connectionMonitor.IsConnected
                  .Subscribe(async obs =>
                 {
                   _isConnected = obs;


                   while (_isConnected && _configuration.RepositoryEventCache.Count > 0)
                   {

                     if (_configuration.RepositoryEventCache.TryPop(out RepositoryCacheItem<TKey> item))
                     {
                       await Save(item.Aggregate);
                     }

                   }

                 });

    }

    public async Task<TAggregate> GetById<TAggregate>(TKey id, bool loadEvents = false) where TAggregate : IAggregate<TKey>, new()
    {
      if (!_isConnected) throw new InvalidOperationException("Client is not connected to EventStore");

      var aggregate = new TAggregate();

      var streamName = $"{id}";

      var eventNumber = 0L;

      StreamEventsSlice currentSlice;

      do
      {
        currentSlice = await _eventStoreConnection.ReadStreamEventsForwardAsync(streamName, eventNumber, _configuration.ReadPageSize, false);

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
          var payload = DeserializeEvent(resolvedEvent.Event);
          aggregate.ApplyEvent(payload, false, loadEvents);
        }

      } while (!currentSlice.IsEndOfStream);

      return aggregate;
    }

    private async Task Save(IAggregate<TKey> aggregate, params KeyValuePair<string, string>[] extraHeaders)
    {

      var streamName = aggregate.ToStreamId();
      var pendingEvents = aggregate.GetPendingEvents();

      var originalVersion = aggregate.Version;

      WriteResult result;

      var commitHeaders = CreateCommitHeaders(aggregate, extraHeaders);
      var eventsToSave = pendingEvents.Select(ev => ToEventData(Guid.NewGuid(), ev, commitHeaders));

      var eventBatches = GetEventBatches(eventsToSave);

      if (eventBatches.Count == 1)
      {
        result = await _eventStoreConnection.AppendToStreamAsync(streamName, originalVersion, eventBatches[0]);
      }
      else
      {
        using var transaction = await _eventStoreConnection.StartTransactionAsync(streamName, originalVersion);

        foreach (var batch in eventBatches)
        {
          await transaction.WriteAsync(batch);
        }

        result = await transaction.CommitAsync();
      }

      aggregate.ClearPendingEvents();

    }

    private IEvent<TKey> DeserializeEvent(RecordedEvent evt)
    {
      var targetType = _eventTypeProvider.GetEventTypeByName(evt.EventType);

      if (null == targetType) throw new InvalidOperationException($"{evt.EventType} cannot be handled");


      return _configuration.Serializer.DeserializeObject(evt.Data, targetType) as IEvent<TKey>;
    }

    private IList<IList<EventData>> GetEventBatches(IEnumerable<EventData> events)
    {
      return events.Batch(_configuration.WritePageSize).Select(x => (IList<EventData>)x.ToList()).ToList();
    }

    protected virtual IDictionary<string, string> GetCommitHeaders(IAggregate<TKey> aggregate)
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

    private IDictionary<string, string> CreateCommitHeaders(IAggregate<TKey> aggregate, KeyValuePair<string, string>[] extraHeaders)
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

      var data = _configuration.Serializer.SerializeObject(@event);

      var eventHeaders = new Dictionary<string, string>(headers)
            {
                {MetadataKeys.EventClrTypeHeader, @event.GetType().AssemblyQualifiedName}
            };
      var metadata = _configuration.Serializer.SerializeObject(eventHeaders);
      var typeName = @event.Name;

      return new EventData(eventId, typeName, true, data, metadata);
    }

    public async Task Apply<TEntity, TEvent>(TEntity aggregate, TEvent ev, params KeyValuePair<string, string>[] extraHeaders)
        where TEntity : IAggregate<TKey>
        where TEvent : IEvent<TKey>, IMutable<TKey, TEntity>
    {

      aggregate.ApplyEvent(ev);

      if (!_isConnected)
      {
        _configuration.RepositoryEventCache.Push(new RepositoryCacheItem<TKey>()
        {
          Aggregate = aggregate,
          Headers = extraHeaders
        });

        return;
      }

      await Save(aggregate, extraHeaders);

    }

    public void Dispose()
    {
      _cleanup.Dispose();
    }
  }
}
