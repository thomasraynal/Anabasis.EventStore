using EventStore.ClientAPI;

namespace Anabasis.EventStore
{
    public interface IEventStore
    {
        IEventStoreConnection Connection { get; }
    }
}