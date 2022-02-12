using Anabasis.Common;
using Anabasis.Common.Configuration;
using DynamicData;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
    public class MarketDataSink : BaseStatelessActor
    {
        private readonly SourceCache<MarketData, string> _currentPrices = new(item => item.EntityId);

        public MarketDataSink(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public MarketDataSink(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public IObservableCache<MarketData, string> CurrentPrices => _currentPrices.AsObservableCache();

        public Task Handle(MarketDataChanged marketDataChanged)
        {

            var optionalAggregate = _currentPrices.Lookup(marketDataChanged.EntityId);

            MarketData aggregate;

            if (!optionalAggregate.HasValue)
            {
                aggregate = new();
                aggregate.SetEntityId(marketDataChanged.EntityId);
            }
            else
            {
                aggregate = optionalAggregate.Value;
            }

            aggregate.ApplyEvent(marketDataChanged, saveAsPendingEvent: false);

            _currentPrices.AddOrUpdate(aggregate);

            return Task.CompletedTask;
        }
    }
}
