using System;

namespace Anabasis.EventStore
{
    public class ConnectionInfo
    {
        public static readonly ConnectionInfo Initial = new ConnectionInfo(ConnectionStatus.Disconnected, 0);

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