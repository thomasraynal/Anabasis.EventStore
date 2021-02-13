namespace Anabasis.EventStore
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