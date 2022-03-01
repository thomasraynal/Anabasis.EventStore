using Anabasis.Common;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo.Bus
{
    public interface IMarketDataBus: IBus
    {
        IDisposable Subscribe(string consumerId, Func<MarketDataBusMessage, Task> subscriber);
    }
}