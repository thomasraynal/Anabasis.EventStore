using Anabasis.Common;
using System;

namespace Anabasis.Common
{
    public class ConnectionInfo
    {
        public static readonly ConnectionInfo InitialDisconnected = new ConnectionInfo(ConnectionStatus.Disconnected, 0);
        public static readonly ConnectionInfo InitialConnected = new ConnectionInfo(ConnectionStatus.Connected, 1);

        public ConnectionInfo(ConnectionStatus status, int connectCount)
        {
            Status = status;
            ConnectCount = connectCount;
        }

        public ConnectionStatus Status { get; }
        public int ConnectCount { get; }

        public override string ToString()
        {
            return $"{Status} - Connect Counter [{ConnectCount}]";
        }
    }
}
