using Anabasis.Tests.Demo;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using NUnit.Framework;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Reactive.Linq;
using Microsoft.AspNetCore.Builder;
using Anabasis.EventStore;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core.Data;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Anabasis.EventStore.Serialization;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Shared;

namespace Anabasis.Tests
{

  public class TestSerializer : DefaultSerializer { }

  public static class TestContext
  {
    public static ClusterVNode ClusterVNode { get; set; }
  }

  public class TestStartup
  {
    public void ConfigureServices(IServiceCollection services)
    {
      TestContext.ClusterVNode = EmbeddedVNodeBuilder
           .AsSingleNode()
           .RunInMemory()
           .OnDefaultEndpoints()
           .Build();

      var isNodeMaster = false;

      TestContext.ClusterVNode.NodeStatusChanged += (sender, args) =>
      {
        isNodeMaster = args.NewVNodeState == VNodeState.Manager;
      };

      TestContext.ClusterVNode.StartAsync(true).Wait();

      var userCredentials = new UserCredentials("admin", "changeit");
      var connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().Build();

      services.AddEventStoreAggregateRepository<Guid, EventStoreAggregateRepository<Guid>>(TestContext.ClusterVNode, connectionSettings, userCredentials, options =>  options.Serializer = new TestSerializer())
              .AddEventStoreCatchupCache<Guid, Item>(TestContext.ClusterVNode, userCredentials, connectionSettings)
              .AddEventStoreCatchupCache<Guid, Blob>(TestContext.ClusterVNode, userCredentials, connectionSettings);

      services.AddSingleton<IConsumer, Consumer>();
      services.AddSingleton<IProducer, Producer>();
      services.AddSingleton<TestBed>();

      services.Scan(scan => scan.FromApplicationDependencies()
                                .AddClasses(classes => classes.AssignableTo<IEntity<Guid>>()).AsImplementedInterfaces());
    }

    public void Configure(IApplicationBuilder app)
    {
    }
  }

  public class TestNetCoreMvc
  {
    private TestServer _testServer;
    private IWebHost _host;
    private TestBed _testBed;

    [OneTimeTearDown]
    public async Task TearDownFixture()
    {
      await TestContext.ClusterVNode.StopAsync();
    }

    [OneTimeSetUp]
    public void SetupFixture()
    {

      var builder = new WebHostBuilder()
                  .UseKestrel()
                  .ConfigureLogging((hostingContext, logging) =>
                  {
                    logging.AddDebug();
                  })
                  .UseStartup<TestStartup>();

      _testServer = new TestServer(builder);
      _host = _testServer.Host;

      _testBed = _host.Services.GetService<TestBed>();

    }

    [Test, Order(0)]
    public Task ShouldCheckDependenciesAreCorrectlyWired()
    {
      var itemsCache = _host.Services.GetService<CatchupEventStoreCache<Guid, Item>>();
      var blobCache = _host.Services.GetService<CatchupEventStoreCache<Guid, Blob>>();
      var repository = _host.Services.GetService<IEventStoreAggregateRepository<Guid>>();
      var configuration = _host.Services.GetService<IEventStoreRepositoryConfiguration>();

      var events = _host.Services.GetServices<IEntity<Guid>>();

      Assert.AreEqual(7, events.Count(), "Should have registered 5 events");

      Assert.IsNotNull(itemsCache, "An IEventStoreCache<Guid, Item, Item> should have been registered");
      Assert.IsNotNull(blobCache, "An IEventStoreCache<Guid, Blob, BlobOutput> should have been registered");
      Assert.IsNotNull(repository, "An IEventStoreRepository<Guid> should have been registered");

      Assert.IsNotNull(configuration, "An IEventStoreRepositoryConfiguration should have been registered");
      Assert.IsNotNull(configuration.Serializer, "IEventStoreRepositoryConfiguration should have a serializer");

      Assert.AreEqual(typeof(TestSerializer), configuration.Serializer.GetType(), $"IEventStoreRepositoryConfiguration should have registered a {typeof(TestSerializer)}");

      return Task.CompletedTask;
    }

    [Test, Order(1)]
    public async Task ShouldProduceAndConsumeACreateEvent()
    {
      var newItemId = _testBed.Producer.Create();

      await Task.Delay(100);

      var cleanup = _testBed.Consumer.OnChange.Connect().Subscribe(observer =>
      {
        var created = observer.FirstOrDefault(item => item.Current.EntityId == newItemId);

        Assert.IsNotNull(created.Current, $"Consumer should have fetched the newly created item {newItemId}");
        Assert.AreEqual(0, created.Current.Version, "Created item should be at version 0");
        Assert.AreEqual(ItemState.Created, created.Current.State, $"Newly created item {newItemId} should have state {ItemState.Created}");

      });

      await Task.Delay(100);

      cleanup.Dispose();
    }

    [Test, Order(2)]
    public async Task ShouldProduceAndConsumeAnUpdateEvent()
    {

      var itemId = _testBed.Producer.Create();

      await Task.Delay(100);

      var cleanup = _testBed.Consumer.OnChange.Connect().Subscribe(observer =>
      {
        var created = observer.FirstOrDefault(item => item.Current.EntityId == itemId);

        Assert.IsNotNull(created.Current, $"Consumer should have fetched the newly created item {itemId}");
        Assert.AreEqual(0, created.Current.Version, "Created item should be at version 0");
        Assert.AreEqual(ItemState.Created, created.Current.State, $"Newly created item {itemId} should have state {ItemState.Created}");

      });

      await Task.Delay(100);

      cleanup.Dispose();

      var newItem = await _testBed.Producer.Get(itemId, false);

      Assert.IsNotNull(newItem, $"Consumer should have fetched the newly created item {itemId}");

      Assert.AreEqual(ItemState.Created, newItem.State, $"Newly created item {itemId} should have state {ItemState.Created}");

      var itemPayload = "XXX";

      _testBed.Producer.Mutate(newItem, itemPayload);

      //give CI some rope...
      await Task.Delay(100);

      cleanup = _testBed.Consumer.OnChange.Connect().Subscribe(observer =>
     {
       var mutated = observer.FirstOrDefault(item => item.Current.EntityId == itemId);

       Assert.IsNotNull(mutated.Current, $"Consumer should have fetched the mutated item {itemId}");
       Assert.AreEqual(1, mutated.Current.Version, "Created item should be at version 1");
       Assert.AreEqual(ItemState.Ready, mutated.Current.State, $"Mutated item {itemId} should have state {ItemState.Ready}");
       Assert.AreEqual(itemPayload, mutated.Current.Payload, $"Mutated item payload should be {itemPayload}");
     });

      cleanup.Dispose();
    }

    [Test, Order(3)]
    public async Task ShouldProduceAndConsumeMultipleUpdates()
    {
      var items = Enumerable.Range(0, 200).Select(_ => _testBed.Producer.Create()).ToList();

      //ensure repository scheduler run...
      await Task.Delay(500);

      var cleanup = _testBed.Consumer.OnChange
          .Connect()
          .FilterEvents(item => item.State == ItemState.Created)
          .Subscribe(changes =>
      {

        var all = items.All(i => changes.Any(c => c.Current.EntityId == i));

        Assert.IsTrue(all, "Not all updated were processed");

      });

      await Task.Delay(100);

      cleanup.Dispose();

    }

    [Test, Order(4)]
    public async Task ShouldCacheEventsOnDeconnection()
    {
      var monitor = _host.Services.GetService<IConnectionStatusMonitor>() as ConnectionStatusMonitor;

      monitor.ForceConnectionStatus(false);

      var repository = _host.Services.GetService<IEventStoreAggregateRepository<Guid>>();

      var items = Enumerable.Range(0, 10).Select(_ => _testBed.Producer.Create()).ToList();

      var cleanup = _testBed.Consumer.OnChange
          .Connect()
          .FilterEvents(item => item.State == ItemState.Created)
          .Subscribe(changes =>
          {

            var all = items.All(i => changes.All(change => change.Current.EntityId != i));

            Assert.IsTrue(all, "Updates were processed - events should have been cached until connection was up again");

          });

      await Task.Delay(100);

      cleanup.Dispose();

      monitor.ForceConnectionStatus(true);

      //give CI some rope...
      await Task.Delay(1000);

      cleanup = _testBed.Consumer.OnChange
          .Connect()
          .FilterEvents(item => item.State == ItemState.Created)
          .Subscribe(changes =>
          {

            var all = items.All(i => changes.Any(c => c.Current.EntityId == i));
            Assert.IsTrue(all, "Not all updated were processed");

          });

      await Task.Delay(100);

      cleanup.Dispose();
    }

    [Test, Order(5)]
    public async Task ShouldHandleFetchEventsOptions()
    {
      var itemId = _testBed.Producer.Create();

      //give CI some rope...
      await Task.Delay(100);

      var item = await _testBed.Producer.Get(itemId, true);

      Assert.AreEqual(1, item.AppliedEvents.Count(), "Only one event should have been applied");
      Assert.AreEqual(typeof(CreateItemEvent), item.AppliedEvents.First().GetType(), $"Event should of type {typeof(CreateItemEvent)}");

      _testBed.Producer.Mutate(item, "XXX");

      //give CI some rope...
      await Task.Delay(100);

      item = await _testBed.Producer.Get(itemId, true);

      Assert.AreEqual(2, item.AppliedEvents.Count(), "Two events should have been applied");
      Assert.AreEqual(typeof(CreateItemEvent), item.AppliedEvents.First().GetType(), $"Event should of type {typeof(CreateItemEvent)}");
      Assert.AreEqual(typeof(UpdateItemPayloadEvent), item.AppliedEvents.ElementAt(1).GetType(), $"Event should of type {typeof(UpdateItemPayloadEvent)}");


      item = await _testBed.Producer.Get(itemId, false);
      Assert.AreEqual(0, item.AppliedEvents.Count(), "No events should have been fetched");
    }


  }
}
