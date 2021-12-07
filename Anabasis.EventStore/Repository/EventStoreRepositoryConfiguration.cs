using Anabasis.Common;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Repository
{
  public class EventStoreRepositoryConfiguration : IEventStoreRepositoryConfiguration
  {
    public EventStoreRepositoryConfiguration(UserCredentials userCredentials = null)
    {
      UserCredentials = userCredentials;
    }

    public int WritePageSize { get; set; } = 500;
    public int ReadPageSize { get; set; } = 500;
    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
  }
}
