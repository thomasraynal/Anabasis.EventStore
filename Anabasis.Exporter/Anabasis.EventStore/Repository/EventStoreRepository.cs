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
  public class EventStoreRepository : IEventStoreRepository, IDisposable
  {
    protected readonly IEventStoreConnection _eventStoreConnection;
    protected readonly IEventStoreRepositoryConfiguration _eventStoreRepositoryConfiguration;
    protected readonly IEventTypeProvider _eventTypeProvider;

    private readonly IDisposable _cleanup;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;

    public bool IsConnected { get; private set; }

    public EventStoreRepository(
        IEventStoreRepositoryConfiguration eventStoreRepositoryConfiguration,
        IEventStoreConnection eventStoreConnection,
        IConnectionStatusMonitor connectionMonitor,
        IEventTypeProvider eventTypeProvider,
        Microsoft.Extensions.Logging.ILogger logger = null)
    {

      _logger = logger ?? new DummyLogger();

      _eventStoreRepositoryConfiguration = eventStoreRepositoryConfiguration;
      _eventStoreConnection = eventStoreConnection;
      _eventTypeProvider = eventTypeProvider;

      _cleanup = connectionMonitor.OnConnected
            .Subscribe(isConnected =>
           {
             IsConnected = isConnected;

           });
    }

    private async Task Save(IEvent[] events, params KeyValuePair<string, string>[] extraHeaders)
    {

      foreach (var eventBatch in events.GroupBy(ev => ev.StreamId))
      {

        var eventsToSave = eventBatch.Select(ev =>
        {
          var commitHeaders = CreateCommitHeaders(ev, extraHeaders);

          return ToEventData(ev.EventID, ev, commitHeaders);

        });

        await SaveEventBatch(eventBatch.Key, ExpectedVersion.Any, eventsToSave.ToArray());
      }

    }

    protected async Task SaveEventBatch(string streamId, int expectedVersion, EventData[] eventsToSave)
    {
      var eventBatches = GetEventBatches(eventsToSave);

      if (eventBatches.Count == 1)
      {
        await _eventStoreConnection.AppendToStreamAsync(streamId, expectedVersion, eventBatches.Single());
      }
      else
      {
        using var transaction = await _eventStoreConnection.StartTransactionAsync(streamId, expectedVersion);

        foreach (var batch in eventBatches)
        {
          await transaction.WriteAsync(batch);
        }

        await transaction.CommitAsync();
      }

    }

    protected List<EventData[]> GetEventBatches(IEnumerable<EventData> events)
    {
      return events.Batch(_eventStoreRepositoryConfiguration.WritePageSize).Select(batch => batch.ToArray()).ToList();
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

    protected IDictionary<string, string> CreateCommitHeaders(object aggregate, KeyValuePair<string, string>[] extraHeaders)
    {
      var commitHeaders = GetCommitHeaders(aggregate);

      foreach (var extraHeader in extraHeaders)
      {
        commitHeaders[extraHeader.Key] = extraHeader.Value;
      }

      return commitHeaders;
    }

    protected EventData ToEventData(Guid eventId, object @event, IDictionary<string, string> headers)
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

    public async Task Emit(IEvent @event, params KeyValuePair<string, string>[] extraHeaders)
    {
      await Save(new[] { @event }, extraHeaders);
    }

    public async Task Emit(IEvent[] events, params KeyValuePair<string, string>[] extraHeaders)
    {
      await Save(events, extraHeaders);
    }

    public void Dispose()
    {
      _cleanup.Dispose();
    }
  }
}
