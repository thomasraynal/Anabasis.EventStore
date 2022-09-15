using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Snapshot
{
    public interface ISnapshotStrategy
    {
        int SnapshotIntervalInEvents { get; }
        bool IsSnapshotRequired(IAggregate aggregate);
    }
}
