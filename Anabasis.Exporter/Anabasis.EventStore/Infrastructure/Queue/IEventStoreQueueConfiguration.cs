using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Infrastructure.Queue
{
  public interface IEventStoreQueueConfiguration<TKey>
  {
    UserCredentials UserCredentials { get; }
    ISerializer Serializer { get; }
  }
}
