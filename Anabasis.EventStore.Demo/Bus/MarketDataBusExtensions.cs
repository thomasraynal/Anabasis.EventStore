using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo.Bus
{
    public static class MarketDataBusExtensions
    {
        public static void SubscribeMarketDataBus(this IActor actor)
        {
            var marketDataBus = actor.GetConnectedBus<IMarketDataBus>();

            var dispose = marketDataBus.Subscribe(actor.Id,(marketDataChanged) =>
             {
                  actor.OnEventReceived(marketDataChanged);

                 return Task.CompletedTask;
             });

            actor.AddDisposable(dispose);
        }
    }
}
