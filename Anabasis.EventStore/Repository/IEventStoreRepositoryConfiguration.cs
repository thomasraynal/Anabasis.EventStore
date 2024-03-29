using Anabasis.Common;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Repository
{
  public interface IEventStoreRepositoryConfiguration
  {
    UserCredentials? UserCredentials { get; set; }
    int WritePageSize { get; set; }
    int ReadPageSize { get; set; }
    ISerializer Serializer { get; set; }
  }
}
