using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Snapshot
{
  public class AggregateSnapshot
  {
    public AggregateSnapshot()
    {

    }
    public AggregateSnapshot(string streamId, string eventFilter, int version, string serializedAggregate)
    {
      StreamId = streamId;
      EventFilter = eventFilter;
      Version = version;
      SerializedAggregate = serializedAggregate;
      Id = Guid.NewGuid();
    }
    public Guid Id { get; set; }
    public string StreamId { get; set; }
    public string EventFilter { get; set; }
    public int Version { get; set; }
    public string SerializedAggregate { get; set; }

    public override bool Equals(object obj)
    {
      return obj is AggregateSnapshot snapshot &&
             StreamId == snapshot.StreamId &&
             EventFilter == snapshot.EventFilter &&
             Version == snapshot.Version;
    }

    public override int GetHashCode()
    {
      return HashCode.Combine(StreamId, EventFilter, Version);
    }
  }
}
