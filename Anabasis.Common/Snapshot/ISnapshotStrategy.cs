using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface ISnapshotStrategy
    {
        int SnapshotIntervalInEvents { get; }
        bool IsSnapshotRequired(IAggregate aggregate);
    }
}
