using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using DynamicData;
using DynamicData.Binding;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Anabasis.Tests
{
  public class TestEventStoreCache
  {

    [Fact]
    public async Task ShouldCreateCatchupCache()
    {
      var node = EmbeddedVNodeBuilder
        .AsSingleNode()
        .RunInMemory()
        .RunProjections(ProjectionType.All)
        .StartStandardProjections()
        .WithWorkerThreads(16)
        .Build();

      await node.StartAsync(true);

      var debugLogger = new DebugLogger();
      var userCredentials = new UserCredentials("admin", "changeit");

      var connectionSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying().Build();

      var connectionOne = EmbeddedEventStoreConnection.Create(node, connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connectionOne, debugLogger);

      var cacheConfiguration = new CatchupEventStoreCacheConfiguration<Guid, SomeDataAggregate>()
      {
        UserCredentials = userCredentials,
        KeepAppliedEventsOnAggregate = true
      };

      var catchUpCache = new CatchupEventStoreCache<Guid, SomeDataAggregate>(
        connectionMonitor,
        cacheConfiguration,
        new DefaultEventTypeProvider<Guid, SomeDataAggregate>((_) => typeof(SomeData)),
        debugLogger);

      var aggregatesOnCache = new ObservableCollectionExtended<SomeDataAggregate>();

      catchUpCache.AsObservableCache()
                     .Connect()
                     .Bind(aggregatesOnCache)
                     .Subscribe();

      var connection = EmbeddedEventStoreConnection.Create(node);
      await connection.ConnectAsync();

      var someData = new SomeData();

      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration<Guid>();

      var connectionTwo = EmbeddedEventStoreConnection.Create(node, connectionSettings);
      var connectionMonitorTwo = new ConnectionStatusMonitor(connectionTwo, debugLogger);

      var repository = new EventStoreRepository<Guid>(
        eventStoreRepositoryConfiguration,
        connectionTwo,
        connectionMonitorTwo,
        new DefaultEventTypeProvider<Guid>((_) => typeof(SomeData)),
        debugLogger);

      var aggregateId = Guid.NewGuid();

      await repository.Emit(new SomeData(aggregateId));

      await Task.Delay(100);

      await repository.Emit(new SomeData(aggregateId));

      await Task.Delay(100);

    }
  }
}
