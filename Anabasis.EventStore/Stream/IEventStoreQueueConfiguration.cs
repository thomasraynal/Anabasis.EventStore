using Anabasis.Common;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Stream
{
  public interface IEventStoreStreamConfiguration
  {
    UserCredentials UserCredentials { get; }
    ISerializer Serializer { get; }
    bool IgnoreUnknownEvent { get; }
  }
}
