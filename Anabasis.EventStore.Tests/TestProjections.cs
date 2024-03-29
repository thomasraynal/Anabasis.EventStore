//using Anabasis.EventStore.Connection;
//using Anabasis.EventStore.Repository;
//using EventStore.ClientAPI;
//using EventStore.ClientAPI.Common.Log;
//using EventStore.ClientAPI.Embedded;
//using EventStore.ClientAPI.Projections;
//using EventStore.ClientAPI.SystemData;
//using EventStore.Common.Options;
//using EventStore.Core;
//using Microsoft.Extensions.Logging;
//using NUnit.Framework;
//using System;
//using System.IO;
//using System.Net;
//using System.Threading.Tasks;

//namespace Anabasis.EventStore.Tests
//{

//    [TestFixture]
//    public class TestProjections
//    {
//        private UserCredentials _userCredentials;
//        private ConnectionSettings _connectionSettings;
//        private LoggerFactory _loggerFactory;
//        private ClusterVNode _clusterVNode;
//        private readonly IPEndPoint _httpEndpoint = new IPEndPoint(IPAddress.Loopback, 2113);
//        private readonly IPEndPoint _tcpEndpoint = new IPEndPoint(IPAddress.Loopback, 1113);

//        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository eventStoreRepository) _repositoryOne;

//        private Guid _correlationId = Guid.NewGuid();
//        private readonly string _streamId = "streamId";

//        [OneTimeSetUp]
//        public async Task Setup()
//        {

//            _userCredentials = new UserCredentials("admin", "changeit");
//            _connectionSettings = ConnectionSettings.Create()
//                .UseDebugLogger()
//                .SetDefaultUserCredentials(_userCredentials)
//                .KeepRetrying()
//                .Build();

//            _loggerFactory = new LoggerFactory();

//            _clusterVNode = EmbeddedVNodeBuilder
//              .AsSingleNode()
//              .RunProjections(ProjectionType.All, 1)
//              .WithWorkerThreads(1)
//              .StartStandardProjections()
//              .WithHttpOn(_httpEndpoint)
//              .WithExternalTcpOn(_tcpEndpoint)
//              .WithEnableAtomPubOverHTTP(true)
//              .Build();

//            await _clusterVNode.StartAsync(true);


//        }

//        [OneTimeTearDown]
//        public async Task TearDown()
//        {
//            await _clusterVNode.StopAsync();
//        }

//        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository eventStoreRepository) CreateEventRepository()
//        {
//            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
//            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
//            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

//            var eventStoreRepository = new EventStoreRepository(
//              eventStoreRepositoryConfiguration,
//              connection,
//              connectionMonitor,
//              _loggerFactory);

//            return (connectionMonitor, eventStoreRepository);
//        }

//        // [Test, Order(0)]
//        public async Task ShouldExecuteAQueryOnStream()
//        {

//            var testProjection = File.ReadAllText("./Projections/testProjection.js");

//            var eventCount = 0;

//            _repositoryOne = CreateEventRepository();

//            for (var i = 0; i < 10; i++)
//            {
//                await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));
//            }

//            var projectionsManager = new ProjectionsManager(
//                log: new ConsoleLogger(),
//                httpEndPoint: _httpEndpoint,
//                operationTimeout: TimeSpan.FromMilliseconds(5000),
//                httpSchema: "http"
//            );

//            await Task.Delay(5000);

//            var all = await projectionsManager.ListAllAsync();

//            await projectionsManager.CreateTransientAsync("countOf", testProjection, _userCredentials);

//            await Task.Delay(100);

//            Assert.AreEqual(1, eventCount);

//        }

//    }
//}
