using Anabasis.Common;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace Anabasis.RabbitMQ.Connection
{
    public class RabbitMqConnectionStatusMonitor : IConnectionStatusMonitor<IRabbitMqConnection>
    {
        private readonly IConnectableObservable<ConnectionInfo> _connectionInfoChanged;
        private readonly IRabbitMqConnection _rabbitMqConnection;
        private readonly Microsoft.Extensions.Logging.ILogger _logger;
        private readonly BehaviorSubject<bool> _isConnected;
        private readonly IDisposable _cleanUp;
        public bool IsConnected => _isConnected.Value;

        public ConnectionInfo ConnectionInfo { get; private set; }

        public IObservable<ConnectionInfo> OnConnectionChanged => _connectionInfoChanged.AsObservable();
        public IObservable<bool> OnConnected => _isConnected.AsObservable();

        public IRabbitMqConnection Connection => _rabbitMqConnection;

        public RabbitMqConnectionStatusMonitor(IRabbitMqConnection connection, ILoggerFactory loggerFactory = null)
        {
            _rabbitMqConnection = connection;

            _logger = loggerFactory?.CreateLogger(nameof(RabbitMqConnectionStatusMonitor));

            _isConnected = new BehaviorSubject<bool>(false);

            _rabbitMqConnection.Connect();

            var blocked = Observable.FromEventPattern<ConnectionBlockedEventArgs>(h => connection.AutoRecoveringConnection.ConnectionBlocked += h, h => connection.AutoRecoveringConnection.ConnectionBlocked -= h).Select(e =>
             {
                 _logger.LogInformation($"AMQP connection blocked - {nameof(ShutdownEventArgs)} => {e.EventArgs?.ToJson()}");

                 return ConnectionStatus.Disconnected;
             });

            var unblocked = Observable.FromEventPattern<EventArgs>(h => connection.AutoRecoveringConnection.ConnectionUnblocked += h, h => connection.AutoRecoveringConnection.ConnectionUnblocked -= h).Select(e =>
            {
                _logger.LogInformation($"AMQP connection unblocked - {nameof(EventArgs)} => {e.EventArgs?.ToJson()}");

                return ConnectionStatus.Connected;
            });

            var shutdowned = Observable.FromEventPattern<ShutdownEventArgs>(h => connection.AutoRecoveringConnection.ConnectionShutdown += h, h => connection.AutoRecoveringConnection.ConnectionShutdown -= h).Select(e =>
            {
                _logger.LogInformation($"AMQP connection shutdown - {nameof(ShutdownEventArgs)} => {e.EventArgs?.ToJson()}");

                return ConnectionStatus.Disconnected;
            });

            var closed = Observable.FromEventPattern<CallbackExceptionEventArgs>(h => connection.AutoRecoveringConnection.CallbackException += h, h => connection.AutoRecoveringConnection.CallbackException -= h).Select(e =>
            {
                _logger.LogError(e.EventArgs.Exception, $"An exception occured in a callback process - {nameof(CallbackExceptionEventArgs)} => {e.EventArgs?.ToJson()}");

                return ConnectionStatus.ErrorOccurred;
            });

            var errorOccurred = Observable.FromEventPattern<EventArgs>(h => connection.AutoRecoveringConnection.RecoverySucceeded += h, h => connection.AutoRecoveringConnection.RecoverySucceeded -= h).Select(e =>
            {
                _logger.LogInformation($"AMQP connection recovery success - {nameof(EventArgs)} => {e.EventArgs?.ToJson()}");

                return ConnectionStatus.Connected;
            });

            var connectionRecoveryError = Observable.FromEventPattern<ConnectionRecoveryErrorEventArgs>(h => connection.AutoRecoveringConnection.ConnectionRecoveryError += h, h => connection.AutoRecoveringConnection.ConnectionRecoveryError -= h).Select(e =>
            {
                _logger.LogError(e.EventArgs.Exception, $"AMQP connection recovery error - {nameof(ConnectionRecoveryErrorEventArgs)} => {e.EventArgs?.ToJson()}");

                return ConnectionStatus.Disconnected;
            });

            _connectionInfoChanged = Observable.Merge(blocked, unblocked, shutdowned, closed, errorOccurred, connectionRecoveryError)
                                               .Scan(ConnectionInfo.InitialConnected, UpdateConnectionInfo)
                                               .StartWith(ConnectionInfo.InitialConnected)
                                               .Do(connectionInfo =>
                                               {

                                                   ConnectionInfo = connectionInfo;
                                                   _logger?.LogInformation($"{nameof(RabbitMqConnectionStatusMonitor)} => ConnectionInfo - {connectionInfo}");
                                                   _isConnected.OnNext(connectionInfo.Status == ConnectionStatus.Connected);

                                               })
                                               .Replay(1);

            _cleanUp = _connectionInfoChanged.Connect();

        }

        public IObservable<IConnected<IRabbitMqConnection>> GetConnectionStatus()
        {
            return _connectionInfoChanged
                          .Where(connectionInfo => connectionInfo.Status == ConnectionStatus.Connected || connectionInfo.Status == ConnectionStatus.Disconnected)
                          .Select(connectionInfo =>
                          {
                              return connectionInfo.Status == ConnectionStatus.Connected ? Connected.Yes(_rabbitMqConnection) : Connected.No<IRabbitMqConnection>();
                          });

        }

        private ConnectionInfo UpdateConnectionInfo(ConnectionInfo previousConnectionInfo, ConnectionStatus connectionStatus)
        {
            ConnectionInfo newConnectionInfo;

            if (connectionStatus == ConnectionStatus.Connected)
            {
                newConnectionInfo = new ConnectionInfo(connectionStatus, previousConnectionInfo.ConnectCount + 1);
            }
            else
            {
                newConnectionInfo = new ConnectionInfo(connectionStatus, previousConnectionInfo.ConnectCount);
            }

            return newConnectionInfo;
        }

        public void Dispose()
        {
            _isConnected.OnCompleted();
            _isConnected.Dispose();
            _cleanUp.Dispose();
        }
    }
}
