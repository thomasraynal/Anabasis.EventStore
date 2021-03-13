using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Infrastructure.Queue
{
  public interface IEventStoreQueueConfiguration
  {
    UserCredentials UserCredentials { get; }
    ISerializer Serializer { get; }
    bool IgnoreUnknownEvent { get; }
  }
}
