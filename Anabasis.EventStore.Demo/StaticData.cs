

using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;

namespace Anabasis.EventStore.Demo
{
    public static class StaticData
    {
        private static readonly ClusterVNode _clusterVNode;

        static StaticData()
        {
            _clusterVNode = EmbeddedVNodeBuilder
                .AsSingleNode()
                .RunInMemory()
                .RunProjections(ProjectionType.All)
                .StartStandardProjections()
                .WithWorkerThreads(1)
                .Build();

            _clusterVNode.StartAsync(true).Wait();

        }

        public static ClusterVNode ClusterVNode => _clusterVNode;

        public static ConnectionSettings GetConnectionSettings()
        {
            return ConnectionSettings.Create()
                  .UseDebugLogger()
                  .SetDefaultUserCredentials(UserCredentials)
                  .KeepRetrying()
                  .Build();
        }

        public static UserCredentials UserCredentials => new("admin", "changeit");

        public static string[] Customers { get; } = new[]
        {
            "Bank of Andorra",
            "Bank of Europe",
            "Bank of England",
            "BCCI",
            "Abbey National",
            "Fx Shop",
            "Midland Bank",
            "National Bank of Alaska",
            "Northern Rock"
        };

        public static CurrencyPair[] CurrencyPairs { get; } = {
            new CurrencyPair("GBP/USD",1.6M,4,5M) ,
            new CurrencyPair("EUR/USD",1.23904M,4,3M),
            new CurrencyPair("EUR/GBP",0.7913M,4,2M),
            new CurrencyPair("NZD/CAD",0.8855M,4,0.5M)  ,
            new CurrencyPair("HKD/USD",0.128908M,6,0.01M) ,
            new CurrencyPair("NOK/SEK",1.10M,3,2M) ,
            new CurrencyPair("XAU/GBP",768.399M,3,0.5M) ,
            new CurrencyPair("USD/JPY",118.81M,2,0.1M),
        };
    }
}
