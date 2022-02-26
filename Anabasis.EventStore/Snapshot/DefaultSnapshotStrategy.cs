using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Snapshot
{
    public class DefaultSnapshotStrategy : ISnapshotStrategy
    {
        public DefaultSnapshotStrategy(int snapshotIntervalInEvents = 10)
        {
            SnapshotIntervalInEvents = snapshotIntervalInEvents;
        }

        public int SnapshotIntervalInEvents { get; }

        public bool IsSnapshotRequired(IAggregate aggregate)
        {
            return aggregate.Version - aggregate.VersionFromSnapshot >= SnapshotIntervalInEvents;
        }
    }
}
