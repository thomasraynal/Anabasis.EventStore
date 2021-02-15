using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
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

      var connectionSettings =  ConnectionSettings.Create().KeepReconnecting().KeepRetrying().Build();

      var connectionOne = EmbeddedEventStoreConnection.Create(node, connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connectionOne, debugLogger);

      var cacheConfiguration = new CatchupEventStoreCacheConfiguration<Guid, EventDataAggregate>()
      {
        UserCredentials = userCredentials
      };

      var catchUpCache = new CatchupEventStoreCache<Guid, EventDataAggregate>(
        connectionMonitor,
        cacheConfiguration,
        new DefaultEventTypeProvider<Guid, EventDataAggregate>((_) => typeof(SomeData)),
        debugLogger);

      catchUpCache.AsObservableCache()
                  .Connect()
                  .Subscribe(ev =>
                  {



                  });

      var connection = EmbeddedEventStoreConnection.Create(node);
      await connection.ConnectAsync();

      var someData = new SomeData()
      {
        Data = "sfsf"
      };

      var data = cacheConfiguration.Serializer.SerializeObject(someData);

      var eventHeaders = new Dictionary<string, string>()
            {
                {MetadataKeys.EventClrTypeHeader, someData.GetType().AssemblyQualifiedName}
            };

      var metadata = cacheConfiguration.Serializer.SerializeObject(eventHeaders);
      var sampleEventData = new EventData(Guid.NewGuid(), $"{someData.GetType()}", true, data, metadata);


      WriteResult writeResult = await connection.AppendToStreamAsync("sampleStream", ExpectedVersion.Any, sampleEventData);
      //   var readEvents = await connection.ReadStreamEventsForwardAsync("sampleStream", 0, 10, true);

      await Task.Delay(1000);
    }

  }
}
