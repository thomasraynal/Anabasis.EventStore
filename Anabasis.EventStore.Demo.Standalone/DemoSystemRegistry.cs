using Anabasis.EventStore.Demo.Bus;
using Lamar;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class DemoSystemRegistry : ServiceRegistry
    {
        public DemoSystemRegistry()
        {
            For<IMarketDataBus>().Use<MarketDataBus>();
        }
    }
}
