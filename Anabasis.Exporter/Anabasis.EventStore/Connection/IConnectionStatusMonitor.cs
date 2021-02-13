using System;
using EventStore.ClientAPI;

namespace Anabasis.EventStore
{
    public interface IConnectionStatusMonitor
    {
        IObservable<bool> IsConnected { get; }
        IObservable<IConnected<IEventStoreConnection>> GetEventStoreConnectedStream();
    }
}