using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo
{
    public class MarketDataActor : SubscribeToAllStreamsEventStoreStatefulActor<MarketData>
    {
        public MarketDataActor(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<MarketData> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public MarketDataActor(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<MarketData> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public async Task Handle(MarketDataQuoteChanged marketDataQuoteChanged)
        {
            var marketDataChanged = new MarketDataChanged(marketDataQuoteChanged.CurrencyPair, marketDataQuoteChanged.CorrelationId)
            {
                Bid = marketDataQuoteChanged.Bid,
                Offer = marketDataQuoteChanged.Offer,
            };

            Logger.LogInformation($"{marketDataChanged}");

            await this.EmitEventStore(marketDataChanged);
        }
    }
}
