using Anabasis.Common;
using System;
using System.Reactive.Linq;

namespace Anabasis.EventHubs
{
    public class EventHubConnectionStatusMonitor : IConnectionStatusMonitor
    {
        public bool IsConnected => true;

        public ConnectionInfo ConnectionInfo => new(ConnectionStatus.Connected, 0);

        public IObservable<bool> OnConnected => Observable.Return<bool>(true);

        public void Dispose()
        {
        }
    }
}
