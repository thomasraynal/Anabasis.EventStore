using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using Anabasis.Tests.Components;
using DynamicData;
using DynamicData.Binding;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Lamar;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Anabasis.Tests
{

  [TestFixture]
  public class TestProjections
  {
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;
    private readonly IPEndPoint _httpEndpoint = new IPEndPoint(IPAddress.Loopback, 2113);
    private readonly IPEndPoint _tcpEndpoint = new IPEndPoint(IPAddress.Loopback, 1113);

    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository eventStoreRepository) _repositoryOne;

    private Guid _correlationId = Guid.NewGuid();
    private readonly string _streamId = "streamId";

    [OneTimeSetUp]
    public async Task Setup()
    {

      _userCredentials = new UserCredentials("admin", "changeit");
      _connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepReconnecting().KeepRetrying().Build();

      _clusterVNode = EmbeddedVNodeBuilder
        .AsSingleNode()
        .RunProjections(ProjectionType.All, 1)
        .WithWorkerThreads(1)
        .StartStandardProjections()
        .WithHttpOn(_httpEndpoint)
        .WithExternalTcpOn(_tcpEndpoint)
        .WithEnableAtomPubOverHTTP(true)
        .Build();

      await _clusterVNode.StartAsync(true);


    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
      await _clusterVNode.StopAsync();
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository eventStoreRepository) CreateEventRepository()
    {
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(_userCredentials);
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var eventStoreRepository = new EventStoreRepository(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeData<string>) }));

      return (connectionMonitor, eventStoreRepository);
    }

   // [Test, Order(0)]
    public async Task ShouldExecuteAQueryOnStream()
    {

      var testProjection = File.ReadAllText("./Projections/testProjection.js");

      var eventCount = 0;

      _repositoryOne = CreateEventRepository();

      for (var i = 0; i < 10; i++)
      {
        await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));
      }

      var projectionsManager = new ProjectionsManager(
          log: new ConsoleLogger(),
          httpEndPoint: _httpEndpoint,
          operationTimeout: TimeSpan.FromMilliseconds(5000),
          httpSchema: "http"
      );

      await Task.Delay(5000);

      var all = await projectionsManager.ListAllAsync();

      await projectionsManager.CreateTransientAsync("countOf", testProjection, _userCredentials);

      await Task.Delay(100);

      Assert.AreEqual(1, eventCount);

    }

  }
}
