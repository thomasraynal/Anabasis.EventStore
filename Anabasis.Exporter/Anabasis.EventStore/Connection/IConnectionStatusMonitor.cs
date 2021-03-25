using System;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;

namespace Anabasis.EventStore.Connection
{
  public interface IConnectionStatusMonitor
  {
    bool IsConnected { get; }
    ConnectionInfo ConnectionInfo { get; }
    IObservable<bool> OnConnected { get; }
    IObservable<IConnected<IEventStoreConnection>> GetEvenStoreConnectionStatus();
  }
}
