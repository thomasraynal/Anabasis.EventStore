using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Connection
{
    public class ConnectionStatusMonitor : IConnectionStatusMonitor
    {
        private readonly IConnectableObservable<ConnectionInfo> _connectionInfoChanged;
        private readonly IEventStoreConnection _eventStoreConnection;
        private readonly Subject<ConnectionStatus> _forceConnectionStatus;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly BehaviorSubject<bool> _isConnected;
        private readonly IDisposable _cleanUp;
        public bool IsConnected => _isConnected.Value;

        public ConnectionInfo ConnectionInfo { get; private set; }

        public IObservable<ConnectionInfo> OnConnectionChanged => _connectionInfoChanged.AsObservable();
        public IObservable<bool> OnConnected => _isConnected.AsObservable();

        public ConnectionStatusMonitor(IEventStoreConnection connection, ILoggerFactory loggerFactory = null)
        {
            _eventStoreConnection = connection;

            _logger = loggerFactory?.CreateLogger(nameof(ConnectionStatusMonitor));

            _forceConnectionStatus = new Subject<ConnectionStatus>();

            _isConnected = new BehaviorSubject<bool>(false);

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
                _logger?.LogWarning($"{nameof(ConnectionStatusMonitor)} => Connection closed - [{arg.EventArgs.Reason}]");

                return ConnectionStatus.Closed;
            });

            var errorOccurred = Observable.FromEventPattern<ClientErrorEventArgs>(h => connection.ErrorOccurred += h, h => connection.ErrorOccurred -= h).Select(arg =>
            {
                _logger?.LogError(arg.EventArgs.Exception, $"{nameof(ConnectionStatusMonitor)} => An error occured while connected to EventStore");

                return ConnectionStatus.ErrorOccurred;
            });

            var authenticationFailed = Observable.FromEventPattern<ClientAuthenticationFailedEventArgs>(h => connection.AuthenticationFailed += h, h => connection.AuthenticationFailed -= h).Select(arg =>
            {
                _logger?.LogError($"{nameof(ConnectionStatusMonitor)} => Authentication failed while connecting to EventStore - [{arg.EventArgs.Reason}]");

                return ConnectionStatus.AuthenticationFailed;
            });

            var forceConnectionStatus =

            _connectionInfoChanged = Observable.Merge(connected, disconnected, reconnecting, closed, errorOccurred, authenticationFailed, _forceConnectionStatus)
                                               .Scan(ConnectionInfo.Initial, UpdateConnectionInfo)
                                               .StartWith(ConnectionInfo.Initial)
                                               .Do(connectionInfo =>
                                               {

                                                   ConnectionInfo = connectionInfo;
                                                   _logger?.LogInformation($"{nameof(ConnectionStatusMonitor)} => ConnectionInfo - {connectionInfo}");
                                                   _isConnected.OnNext(connectionInfo.Status == ConnectionStatus.Connected);

                                               })
                                               .Replay(1);

            _cleanUp = _connectionInfoChanged.Connect();

            _eventStoreConnection.ConnectAsync().Wait();
        }


        public void Dispose()
        {
            _isConnected.OnCompleted();
            _isConnected.Dispose();
            _cleanUp.Dispose();
        }

        //for testing purpose
        public void ForceConnectionStatus(bool isConnected)
        {
            _forceConnectionStatus.OnNext(isConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected);
        }

        public IObservable<IConnected<IEventStoreConnection>> GetEvenStoreConnectionStatus()
        {
            return _connectionInfoChanged
                          .Where(connectionInfo => connectionInfo.Status == ConnectionStatus.Connected || connectionInfo.Status == ConnectionStatus.Disconnected)
                          .Select(connectionInfo =>
                          {
                              return connectionInfo.Status == ConnectionStatus.Connected ? Connected.Yes(_eventStoreConnection) : Connected.No<IEventStoreConnection>();
                          });

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
