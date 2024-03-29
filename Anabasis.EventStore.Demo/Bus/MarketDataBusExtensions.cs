﻿using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo.Bus
{
    public static class MarketDataBusExtensions
    {
        public static void SubscribeMarketDataBus(this IAnabasisActor actor)
        {
            var marketDataBus = actor.GetConnectedBus<IMarketDataBus>();

            var dispose = marketDataBus.Subscribe(actor.Id,(marketDataChanged) =>
             {
                  actor.OnMessageReceived(marketDataChanged);

                 return Task.CompletedTask;
             });

            actor.AddToCleanup(dispose);
        }
    }
}
