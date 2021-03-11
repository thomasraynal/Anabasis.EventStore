using Anabasis.Actor;
using Anabasis.Actor.Actor;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Queue;
using Anabasis.EventStore.Infrastructure.Queue.PersistentQueue;
using Anabasis.EventStore.Infrastructure.Queue.VolatileQueue;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Lamar;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace Anabasis.Tests
{

  [TestFixture]
  public class Debugsdfsdf
  {

    private DebugLogger _debugLogger;
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;

    private Guid _correlationId = Guid.NewGuid();
    private readonly string _streamId = "streamId";
    private readonly string _streamId2 = "streamId2";
    private readonly string _groupIdOne = "groupIdOne";
    private readonly string _groupIdTwo = "groupIdTwo";

    [OneTimeSetUp]
    public async Task Setup()
    {

      _debugLogger = new DebugLogger();
      _userCredentials = new UserCredentials("admin", "changeit");
      _connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().Build();

      _clusterVNode = EmbeddedVNodeBuilder
        .AsSingleNode()
        .RunInMemory()
        .RunProjections(ProjectionType.All)
        .StartStandardProjections()
        .WithWorkerThreads(1)
        .Build();

      await _clusterVNode.StartAsync(true);

      await CreateSubscriptionGroups();

    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
      await _clusterVNode.StopAsync();
    }

    private async Task CreateSubscriptionGroups()
    {
      var connectionSettings = PersistentSubscriptionSettings.Create().StartFromCurrent().Build();
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode);

      await connection.CreatePersistentSubscriptionAsync(
           _streamId,
           _groupIdOne,
           connectionSettings,
           _userCredentials);

      await connection.CreatePersistentSubscriptionAsync(
           _streamId,
           _groupIdTwo,
           connectionSettings,
           _userCredentials);

      await connection.CreatePersistentSubscriptionAsync(
           _streamId2,
           _groupIdOne,
           connectionSettings,
           _userCredentials);
    }


    [Test, Order(0)]
    public async Task ShouldBuildAndRunActors()
    {

      var testActorAutoBuildOne = ActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                                                                   .WithPersistentSubscriptionQueue(_streamId, _groupIdOne)
                                                                                   .Build();

      var testActorAutoBuildTwo = ActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                                                                   .WithPersistentSubscriptionQueue(_streamId, _groupIdOne)
                                                                                   .Build();


      await testActorAutoBuildOne.Emit(new SomeMoreData(_correlationId, _streamId));

      await Task.Delay(1000);

      Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);
    }
  }
}
