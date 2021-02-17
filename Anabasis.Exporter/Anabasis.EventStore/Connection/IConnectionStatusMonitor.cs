using System;
using EventStore.ClientAPI;

namespace Anabasis.EventStore
{
  public interface IConnectionStatusMonitor
  {
    bool IsConnected { get; }
    ConnectionInfo ConnectionInfo { get; }
    IObservable<bool> OnConnected { get; }
    IObservable<IConnected<IEventStoreConnection>> GetEvenStoreConnectionStatus();
  }
}
