using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

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
