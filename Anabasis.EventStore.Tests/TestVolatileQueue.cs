using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    [TestFixture]
    public class TestVolatileStream
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;
        private (ConnectionStatusMonitor connectionStatusMonitor, SubscribeFromEndEventStoreStream volatileEventStoreStream) _streamOne;
        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository eventStoreRepository) _repositoryOne;
        private (ConnectionStatusMonitor connectionStatusMonitor, SubscribeFromEndEventStoreStream volatileEventStoreStream) _streamTwo;

        private Guid _correlationId = Guid.NewGuid();

        [OneTimeSetUp]
        public async Task Setup()
        {

            _userCredentials = new UserCredentials("admin", "changeit");
            _connectionSettings = ConnectionSettings.Create()
                .UseDebugLogger()
                .SetDefaultUserCredentials(_userCredentials)
                .KeepRetrying()
                .Build();

            _loggerFactory = new LoggerFactory();

            _clusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .RunProjections(ProjectionType.All)
              .StartStandardProjections()
              .WithWorkerThreads(1)
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
            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            return (connectionMonitor, eventStoreRepository);
        }

        private (ConnectionStatusMonitor connectionStatusMonitor, SubscribeFromEndEventStoreStream volatileEventStoreStream) CreateVolatileEventStoreStream()
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var volatileEventStoreStreamConfiguration = new SubscribeFromEndEventStoreStreamConfiguration(_userCredentials);

            var volatileEventStoreStream = new SubscribeFromEndEventStoreStream(
              connectionMonitor,
              volatileEventStoreStreamConfiguration,
              new DefaultEventTypeProvider(() => new[] { typeof(SomeRandomEvent) }),
              _loggerFactory);

            volatileEventStoreStream.Connect();

            return (connectionMonitor, volatileEventStoreStream);

        }

        [Test, Order(0)]
        public async Task ShouldCreateAndRunAVolatileEventStoreStream()
        {
            _streamOne = CreateVolatileEventStoreStream();

            await Task.Delay(100);

            Assert.IsTrue(_streamOne.volatileEventStoreStream.IsConnected);

        }

        [Test, Order(1)]
        public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
        {

            var eventCount = 0;

            _streamOne.volatileEventStoreStream.OnEvent().Subscribe((@event) =>
            {
                eventCount++;
            });

            _repositoryOne = CreateEventRepository();

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(100);

            Assert.AreEqual(1, eventCount);

        }

        [Test, Order(2)]
        public async Task ShouldDropConnectionAndReinitializeIt()
        {

            var eventCount = 0;

            await _clusterVNode.StopAsync();

            _streamOne.volatileEventStoreStream.OnEvent().Subscribe((@event) =>
             {
                 eventCount++;
             });

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(500);

            Assert.AreEqual(0, eventCount);

            await _clusterVNode.StartAsync(true);

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(500);

            Assert.AreEqual(1, eventCount);

        }

        [Test, Order(3)]
        public async Task ShouldCreateASecondCacheAndCatchEvents()
        {
            var eventCountOne = 0;
            var eventCountTwo = 0;

            _streamOne.volatileEventStoreStream.OnEvent().Subscribe((@event) =>
            {
                eventCountOne++;
            });

            _streamTwo = CreateVolatileEventStoreStream();

            _streamTwo.volatileEventStoreStream.OnEvent().Subscribe((@event) =>
            {
                eventCountTwo++;
            });

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(100);

            Assert.AreEqual(1, eventCountOne);
            Assert.AreEqual(1, eventCountTwo);

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(100);

            Assert.AreEqual(2, eventCountOne);
            Assert.AreEqual(2, eventCountTwo);
        }
    }
}
