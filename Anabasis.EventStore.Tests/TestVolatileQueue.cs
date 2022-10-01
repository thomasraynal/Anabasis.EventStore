using Anabasis.EventStore.Connection;
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
using Anabasis.Common;

namespace Anabasis.EventStore.Tests
{
    [TestFixture]
    public class TestVolatileStream
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;
        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, SubscribeToAllEventStoreStream volatileEventStoreStream) _streamOne;
        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository eventStoreRepository) _repositoryOne;
        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, SubscribeToAllEventStoreStream volatileEventStoreStream) _streamTwo;

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

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository eventStoreRepository) CreateEventRepository()
        {
            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            return (connectionMonitor, eventStoreRepository);
        }

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, SubscribeToAllEventStoreStream volatileEventStoreStream) CreateVolatileEventStoreStream()
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

            var volatileEventStoreStreamConfiguration = new SubscribeToAllStreamsConfiguration(Position.Start, _userCredentials);

            var volatileEventStoreStream = new SubscribeToAllEventStoreStream(
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

            _streamOne.volatileEventStoreStream.OnMessage().Subscribe((@event) =>
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

            _streamOne.volatileEventStoreStream.Disconnect();

            await Task.Delay(200);

            _streamOne.volatileEventStoreStream.OnMessage().Subscribe((@event) =>
             {
                 eventCount++;
             });

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(200);

            Assert.AreEqual(0, eventCount);

            _streamOne.volatileEventStoreStream.Connect();

            await Task.Delay(200);

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(200);

            Assert.AreEqual(1, eventCount);

        }

        [Test, Order(3)]
        public async Task ShouldCreateASecondCacheAndCatchEvents()
        {
            var eventCountOne = 0;
            var eventCountTwo = 0;

            _streamOne.volatileEventStoreStream.OnMessage().Subscribe((@event) =>
            {
                eventCountOne++;
            });

            _streamTwo = CreateVolatileEventStoreStream();

            _streamTwo.volatileEventStoreStream.OnMessage().Subscribe((@event) =>
            {
                eventCountTwo++;
            });

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(200);

            Assert.AreEqual(1, eventCountOne);
            Assert.AreEqual(1, eventCountTwo);

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId));

            await Task.Delay(200);

            Assert.AreEqual(2, eventCountOne);
            Assert.AreEqual(2, eventCountTwo);
        }
    }
}
