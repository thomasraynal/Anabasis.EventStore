using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.ClientAPI;
using ResolvedEvent = EventStore.ClientAPI.ResolvedEvent;

namespace Anabasis.EventStore.Integration.Tests
{

    [TestFixture, Category("Integration")]
    public class ProjectionIntegrationTests
    {
        private DockerEventStoreFixture _dockerEventStoreFixture;

        private readonly IPEndPoint _httpEndpoint = new IPEndPoint(IPAddress.Loopback, 2113);

        [OneTimeSetUp]
        public async Task SetUp()
        {

            _dockerEventStoreFixture = new DockerEventStoreFixture();

            await _dockerEventStoreFixture.Initialize();
        }


        [Test, Order(0)]
        public async Task ShouldCreateAndRunAndDeleteAProjection()
        {

            var userCredentials = new UserCredentials("admin", "changeit");
            var esdbUri = "esdb://admin:changeit@localhost:2113";
            var tcpUri = "tcp://admin:changeit@localhost:1113";

            await Task.Delay(5000);

            var eventStoreClientSettings = EventStoreClientSettings.Create(esdbUri);

            eventStoreClientSettings.DefaultCredentials = userCredentials;

            var eventStoreProjectionManagementClient = new EventStoreProjectionManagementClient(eventStoreClientSettings);

            var testProjection = File.ReadAllText("./Projections/testProjection.js");

            var allOne = await eventStoreProjectionManagementClient.ListAllAsync().ToArrayAsync();

            Assert.Greater(allOne.Length, 0);

            await eventStoreProjectionManagementClient.CreateOneTimeAsync(testProjection, userCredentials);

            var allTwo = await eventStoreProjectionManagementClient.ListAllAsync().ToArrayAsync();

            Action<ResolvedEvent> handler = (ev) => { };

            var oneTimeSubs = await eventStoreProjectionManagementClient.ListOneTimeAsync().ToArrayAsync();

            var subName = oneTimeSubs.First().Name;

            Assert.AreEqual(allTwo.Length, allOne.Length + 1);

            ProjectionDetails projectionDetails;
            var timeout = DateTime.UtcNow.AddSeconds(5);
            bool isCompleted;
            do
            {
                await Task.Delay(50);
                projectionDetails = await eventStoreProjectionManagementClient.GetStatusAsync(subName);
                isCompleted = false;// projectionDetails.IsCompletedWithResults();
                if (!isCompleted) await Task.Delay(200);
            } while (!isCompleted && timeout > DateTime.UtcNow);

            if (!isCompleted)
            {
                throw new Exception($"Query timed out, status:'{ projectionDetails.Status }' - { projectionDetails.StateReason }");
            }

            var exceptions = new ConcurrentBag<Exception>();
            var finished = false;
            var error = false;

            var connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().DisableTls().Build();
            var connection = EventStoreConnection.Create(connectionSettings, new Uri(tcpUri));

            await connection.ConnectAsync();

            var subscription = connection.SubscribeToStreamFrom(
                $"$projections-{subName}-result",
                StreamCheckpoint.StreamStart,
                new CatchUpSubscriptionSettings(maxLiveQueueSize: CatchUpSubscriptionSettings.Default.MaxLiveQueueSize, readBatchSize: CatchUpSubscriptionSettings.Default.ReadBatchSize, verboseLogging: CatchUpSubscriptionSettings.Default.VerboseLogging, resolveLinkTos: false),
                (sub, ev) =>
                {
                    if (ev.Event.EventType == "$Eof") { finished = true; return; }
                    if (ev.Event.Data.Length == 0) return;
                    try { handler(ev); }
                    catch (Exception ex) { exceptions.Add(ex); }
                }, subscriptionDropped: (sub, label, ex) =>
            {
                    finished = true;
                    error = true;
                });

            try
            {
                while (!(finished || error || exceptions.Count > 0)) await Task.Delay(100);
            }
            finally
            {

                Assert.True(finished);
                Assert.False(error);
                Assert.True(exceptions.Count == 0);

                subscription.Stop();
            }





        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _dockerEventStoreFixture.Dispose();
        }
    }

}
