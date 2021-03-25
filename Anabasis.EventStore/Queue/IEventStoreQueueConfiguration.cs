using Anabasis.EventStore.Serialization;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Queue
{
  public interface IEventStoreQueueConfiguration
  {
    UserCredentials UserCredentials { get; }
    ISerializer Serializer { get; }
    bool IgnoreUnknownEvent { get; }
  }
}
