using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
using Anabasis.EventStore.Infrastructure.Repository;
using DynamicData;
using DynamicData.Binding;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.InMemory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Tests
{
  public class TestSink : ILogEventSink
  {

    public List<LogEvent> Logs = new List<LogEvent>();

    public void Emit(LogEvent logEvent)
    {
      Logs.Add(logEvent);
    }
  }

  public static class SinkExtensions
  {
    public static TestSink TestSink = new TestSink();

    public static LoggerConfiguration UseTestSink(
              this LoggerSinkConfiguration loggerConfiguration)
    {
      return loggerConfiguration.Sink(TestSink);
    }
  }

  [TestFixture]
  public class TestLogging
  {
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;

    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _cacheOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<Guid> eventStoreRepository) _repositoryOne;

    private Guid _firstAggregateId;
    private ILoggerFactory _loggerFactory;

    [OneTimeSetUp]
    public async Task Setup()
    {

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

      Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
        .WriteTo.Debug()
        .WriteTo.UseTestSink()
        .CreateLogger();

      _loggerFactory = new LoggerFactory();
      _loggerFactory.AddSerilog(Log.Logger);

    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
      await _clusterVNode.StopAsync();
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<Guid> eventStoreRepository) CreateEventRepository()
    {
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(_userCredentials);
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var eventStoreRepository = new EventStoreAggregateRepository<Guid>(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeData<Guid>) }),
        logger: _loggerFactory.CreateLogger<CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>>>());

      return (connectionMonitor, eventStoreRepository);
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) CreateCatchupEventStoreCache()
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory.CreateLogger<ConnectionStatusMonitor>());

      var cacheConfiguration = new CatchupEventStoreCacheConfiguration<Guid, SomeDataAggregate<Guid>>(_userCredentials)
      {
        UserCredentials = _userCredentials,
        KeepAppliedEventsOnAggregate = true,
        IsStaleTimeSpan = TimeSpan.FromSeconds(1)
      };

      var catchUpCache = new CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>>(
        connectionMonitor,
        cacheConfiguration,
       new DefaultEventTypeProvider<Guid, SomeDataAggregate<Guid>>(() => new[] { typeof(SomeData<Guid>) }),
       logger: _loggerFactory.CreateLogger<CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>>>());

      var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate<Guid>>();

      catchUpCache.AsObservableCache()
                     .Connect()
                     .Bind(aggregatesOnCacheOne)
                     .Subscribe();

      return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

    }


    [Test, Order(0)]
    public async Task ShouldCreateAndRunACatchupEventStoreCacheAndLogSomething()
    {
       _cacheOne = CreateCatchupEventStoreCache();
      _repositoryOne = CreateEventRepository();

      await Task.Delay(100);

      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

      await Task.Delay(500);

      Assert.IsTrue(SinkExtensions.TestSink.Logs.Count > 0);

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId));

      await Task.Delay(500);

      Assert.IsTrue(SinkExtensions.TestSink.Logs.Any(Log => Log.MessageTemplate.Text.Contains("OnEvent")));
      Assert.IsTrue(SinkExtensions.TestSink.Logs.Any(Log=> Log.MessageTemplate.Text.Contains("OnResolvedEvent")));
    }
  }
}
