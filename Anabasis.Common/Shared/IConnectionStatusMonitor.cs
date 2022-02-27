using System;
using Anabasis.Common;

namespace Anabasis.Common
{

    public interface IConnectionStatusMonitor : IDisposable
    {
        bool IsConnected { get; }
        ConnectionInfo ConnectionInfo { get; }
        IObservable<bool> OnConnected { get; }
    }

    public interface IConnectionStatusMonitor<TConnection> : IConnectionStatusMonitor
    {
        TConnection Connection { get; }
        IObservable<IConnected<TConnection>> GetConnectionStatus();
    }
}
