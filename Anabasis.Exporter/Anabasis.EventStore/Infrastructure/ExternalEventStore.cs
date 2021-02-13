using System;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using EventStore.Core.Data;

namespace Anabasis.EventStore
{
  public class ExternalEventStore : IEventStore
  {
    public ExternalEventStore(string url)
    {
      Connection = EventStoreConnection.Create(EventStoreConnectionSettings.Default, new Uri(url));
    }

    public ExternalEventStore(string url, ConnectionSettings settings)
    {
      Connection = EventStoreConnection.Create(settings, new Uri(url));
    }

    public ExternalEventStore(ClusterVNode clusterVNode, ConnectionSettings settings)
    {
      Connection = EmbeddedEventStoreConnection.Create(clusterVNode, settings);
    }

    public ExternalEventStore(IEventStoreConnection connection)
    {
      Connection = connection;
    }

    public IEventStoreConnection Connection { get; }
  }
}
