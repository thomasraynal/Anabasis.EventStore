using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore
{
  public class EventStoreRepositoryConfiguration<TKey> : IEventStoreRepositoryConfiguration<TKey>
  {
    public EventStoreRepositoryConfiguration(UserCredentials userCredentials, ConnectionSettings connectionSettings)
    {
      UserCredentials = userCredentials;
      ConnectionSettings = connectionSettings;
    }

    public int WritePageSize { get; set; } = 500;
    public int ReadPageSize { get; set; } = 500;
    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
    public ConnectionSettings ConnectionSettings { get; set; }
  }
}
