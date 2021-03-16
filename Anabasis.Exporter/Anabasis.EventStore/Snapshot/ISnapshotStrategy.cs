using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Snapshot
{
  public interface ISnapshotStrategy<TKey>
  {
    int SnapshotIntervalInEvents { get; }
    bool IsSnapShotRequired(IAggregate<TKey> aggregate);
  }
}
