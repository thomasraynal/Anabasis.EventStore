using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore
{
  public interface IEventStoreRepositoryConfiguration<TKey>
  {
    UserCredentials UserCredentials { get; set; }
    ConnectionSettings ConnectionSettings { get; set; }
    int WritePageSize { get; set; }
    int ReadPageSize { get; set; }
    ISerializer Serializer { get; set; }
  }
}
