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
    public class TestPersistentStream
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;
        private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreStream persistentEventStoreStream) _streamOne;
        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository eventStoreRepository) _repositoryOne;
        private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreStream persistentEventStoreStream) _streamTwo;

        private Guid _correlationId = Guid.NewGuid();
        private readonly string _streamId = "streamId";
        private readonly string _groupIdOne = "groupIdOne";
        private readonly string _groupIdTwo = "groupIdTwo";

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

            await CreateSubscriptionGroups();

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

        private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreStream persistentEventStoreStream) CreatePersistentEventStoreStream(string streamId, string groupId)
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var persistentEventStoreStreamConfiguration = new PersistentSubscriptionEventStoreStreamConfiguration(streamId, groupId, _userCredentials);

            var persistentSubscriptionEventStoreStream = new PersistentSubscriptionEventStoreStream(
              connectionMonitor,
              persistentEventStoreStreamConfiguration,
              new DefaultEventTypeProvider(() => new[] { typeof(SomeRandomEvent) }),
              _loggerFactory);

            persistentSubscriptionEventStoreStream.Connect();

            return (connectionMonitor, persistentSubscriptionEventStoreStream);

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
        }


        [Test, Order(0)]
        public async Task ShouldCreateAndRunAVolatileEventStoreStream()
        {

            _streamOne = CreatePersistentEventStoreStream(_streamId, _groupIdOne);

            await Task.Delay(100);

            Assert.IsTrue(_streamOne.persistentEventStoreStream.IsConnected);

        }

        [Test, Order(1)]
        public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
        {

            var eventCount = 0;

            _streamOne.persistentEventStoreStream.OnEvent().Subscribe((@event) =>
            {
                eventCount++;
            });

            _repositoryOne = CreateEventRepository();

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));

            await Task.Delay(100);

            Assert.AreEqual(1, eventCount);

        }

        [Test, Order(2)]
        public async Task ShouldDropConnectionAndReinitializeIt()
        {

            var eventCount = 0;

            _streamOne.persistentEventStoreStream.Disconnect();

            await Task.Delay(200);

            _streamOne.persistentEventStoreStream.OnEvent().Subscribe((@event) =>
            {
                eventCount++;
            });

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));

            await Task.Delay(200);

            Assert.AreEqual(0, eventCount);

            _streamOne.persistentEventStoreStream.Connect();

            await Task.Delay(200);

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));

            await Task.Delay(200);

            Assert.AreEqual(2, eventCount);

        }

        [Test, Order(3)]
        public async Task ShouldCreateASecondCacheAndCatchEvents()
        {
            var eventCountOne = 0;
            var eventCountTwo = 0;

            _streamOne.persistentEventStoreStream.OnEvent().Subscribe((@event) =>
            {
                eventCountOne++;
            });

            _streamTwo = CreatePersistentEventStoreStream(_streamId, _groupIdTwo);

            _streamTwo.persistentEventStoreStream.OnEvent().Subscribe((@event) =>
            {
                eventCountTwo++;
            });

            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));
            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));
            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));
            await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_correlationId, _streamId));

            await Task.Delay(100);

            Assert.True(eventCountOne > 0);
            Assert.True(eventCountTwo > 0);

        }
    }
}

