using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Cache
{
  public class VolatileCacheConfiguration<TKey, TCacheItem> : IEventStoreCacheConfiguration<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {
    public string StreamId { get; set; }
    public ISerializer Serializer { get; set; }
    public UserCredentials UserCredentials { get; set; }
  }
}
