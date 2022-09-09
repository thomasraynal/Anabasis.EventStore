using Anabasis.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Anabasis.EventHubs
{
    public class EventHubConnectionStatusMonitor : IConnectionStatusMonitor
    {
        private readonly ILogger? _logger;
        private readonly IDisposable _checkHealthObservable;
        private readonly BehaviorSubject<bool> _onConnected;

        public EventHubConnectionStatusMonitor(IEventHubBus eventHubBus, ILoggerFactory? loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger(nameof(EventHubConnectionStatusMonitor));

            _onConnected = new BehaviorSubject<bool>(false);

            _checkHealthObservable = Observable.Interval(TimeSpan.FromSeconds(5)).StartWith(0).Subscribe(async _ =>
            {
                var (isConnected, exception) = await eventHubBus.CheckConnectivity();

                if (!isConnected)
                {
                    _logger?.LogInformation($"EventHub diconnected - {exception?.Message}");
                }

                var connectionStatus = isConnected ? ConnectionStatus.Connected : ConnectionStatus.Disconnected;

                ConnectionInfo = UpdateConnectionInfo(ConnectionInfo, connectionStatus);

                _logger?.LogInformation($"{nameof(EventHubConnectionStatusMonitor)} => ConnectionInfo - {connectionStatus}");

            });

        }

        public bool IsConnected => ConnectionInfo.Status == ConnectionStatus.Connected;

        public ConnectionInfo ConnectionInfo { get; private set; } = ConnectionInfo.InitialDisconnected;

        public IObservable<bool> OnConnected => _onConnected.AsObservable();

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
            _checkHealthObservable.Dispose();
        }
    }
}
