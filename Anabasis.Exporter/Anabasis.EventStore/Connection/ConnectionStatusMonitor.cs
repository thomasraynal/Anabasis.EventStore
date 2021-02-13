using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Anabasis.EventStore.Infrastructure;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore
{

  public class ConnectionStatusMonitor : IConnectionStatusMonitor
  {
    private readonly IConnectableObservable<ConnectionInfo> _connectionInfoChanged;
    private readonly IDisposable _connection;
    private readonly IEventStoreConnection _eventStoreConnection;
    private readonly ILogger<ConnectionStatusMonitor> _logger;
    private BehaviorSubject<bool> _isConnected;

    public IObservable<bool> IsConnected
    {
      get
      {
        return _isConnected.AsObservable();
      }
    }

    //refacto - for testing purpose, until a proper embedded EventStore test bed
    public void Disconnect(bool isDisconnected)
    {
      _isConnected.OnNext(!isDisconnected);
    }

    public ConnectionStatusMonitor(IEventStoreConnection connection, ILogger<ConnectionStatusMonitor> logger = null)
    {

      _logger = logger ?? new DummyLogger<ConnectionStatusMonitor>();

      _isConnected = new BehaviorSubject<bool>(false);

      connection.ConnectAsync().Wait();

      var connected = Observable.FromEventPattern<ClientConnectionEventArgs>(h => connection.Connected += h, h => connection.Connected -= h).Select(_ =>
      {
        return ConnectionStatus.Connected;
      });

      var disconnected = Observable.FromEventPattern<ClientConnectionEventArgs>(h => connection.Disconnected += h, h => connection.Disconnected -= h).Select(_ =>
      {
        return ConnectionStatus.Disconnected;
      });

      var reconnecting = Observable.FromEventPattern<ClientReconnectingEventArgs>(h => connection.Reconnecting += h, h => connection.Reconnecting -= h).Select(_ =>
      {
        return ConnectionStatus.Connecting;
      });

      var closed = Observable.FromEventPattern<ClientClosedEventArgs>(h => connection.Closed += h, h => connection.Closed -= h).Select(arg =>
      {
        _logger.LogWarning($"Connection closed - [{arg.EventArgs.Reason}]");
        return ConnectionStatus.Closed;
      });

      var errorOccurred = Observable.FromEventPattern<ClientErrorEventArgs>(h => connection.ErrorOccurred += h, h => connection.ErrorOccurred -= h).Select(arg =>
      {
        _logger.LogError(arg.EventArgs.Exception, "An error occured while connected to EventStore");
        return ConnectionStatus.ErrorOccurred;
      });

      var authenticationFailed = Observable.FromEventPattern<ClientAuthenticationFailedEventArgs>(h => connection.AuthenticationFailed += h, h => connection.AuthenticationFailed -= h).Select(arg =>
       {
         _logger.LogError($"Authentication failed while connecting to EventStore - [{arg.EventArgs.Reason}]");
         return ConnectionStatus.AuthenticationFailed;
       });

      _connectionInfoChanged = Observable.Merge(connected, disconnected, reconnecting, closed, errorOccurred, authenticationFailed)
                                         .Scan(ConnectionInfo.Initial, UpdateConnectionInfo)
                                         .StartWith(ConnectionInfo.Initial)
                                         .Do(c => _logger.LogInformation($"{c}"))
                                         .Do(c => _isConnected.OnNext(c.Status == ConnectionStatus.Connected))
                                         .Replay(1);

      _connection = _connectionInfoChanged.Connect();

      _eventStoreConnection = connection;

    }

    public void Dispose()
    {
      _connection.Dispose();
    }

    public IObservable<IConnected<IEventStoreConnection>> GetEventStoreConnectedStream()
    {
      return _connectionInfoChanged
                    .Where(con => con.Status == ConnectionStatus.Connected || con.Status == ConnectionStatus.Disconnected)
                    .Select(con => con.Status == ConnectionStatus.Connected ? Connected.Yes(_eventStoreConnection) : Connected.No<IEventStoreConnection>());
    }

    private ConnectionInfo UpdateConnectionInfo(ConnectionInfo previousConnectionInfo, ConnectionStatus connectionStatus)
    {
      ConnectionInfo newConnectionInfo;

      if ((previousConnectionInfo.Status == ConnectionStatus.Disconnected || previousConnectionInfo.Status == ConnectionStatus.Connecting) && connectionStatus == ConnectionStatus.Connected)
      {
        newConnectionInfo = new ConnectionInfo(connectionStatus, previousConnectionInfo.ConnectCount + 1);
      }
      else
      {
        newConnectionInfo = new ConnectionInfo(connectionStatus, previousConnectionInfo.ConnectCount);
      }

      return newConnectionInfo;
    }
  }
}
