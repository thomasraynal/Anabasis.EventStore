using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Common.Options;
using EventStore.Core;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Tests.Mvc
{
    public class TestBed
    {
        public ClusterVNode ClusterVNode { get; }
        public UserCredentials UserCredentials { get; }
        public ConnectionSettings ConnectionSettings { get; }

        public TestBed()
        {
            UserCredentials = new UserCredentials("admin", "changeit");

            ConnectionSettings = ConnectionSettings.Create()
                .UseDebugLogger()
                .SetDefaultUserCredentials(UserCredentials)
                .KeepRetrying()
                .Build();


            ClusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .RunProjections(ProjectionType.All)
              .StartStandardProjections()
              .WithWorkerThreads(1)
              .Build();

        }
        public async Task Start()
        {
            await ClusterVNode.StartAsync(true);
        }

        public async Task Stop()
        {
            await ClusterVNode.StopAsync();
        }
    }

}
