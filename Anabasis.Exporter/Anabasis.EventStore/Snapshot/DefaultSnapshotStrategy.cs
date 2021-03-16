using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Snapshot
{
  public class DefaultSnapshotStrategy<TKey> : ISnapshotStrategy<TKey>
  {
    public int SnapshotIntervalInEvents => 10;

    public bool IsSnapShotRequired(IAggregate<TKey> aggregate)
    {
      return aggregate.Version - aggregate.VersionSnapShot >= SnapshotIntervalInEvents;
    }
  }
}
