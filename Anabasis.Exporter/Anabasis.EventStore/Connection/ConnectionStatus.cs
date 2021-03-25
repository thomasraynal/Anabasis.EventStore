namespace Anabasis.EventStore.Connection
{
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Closed,
        ErrorOccurred,
        AuthenticationFailed
    }
}
