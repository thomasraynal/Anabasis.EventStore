using Anabasis.EventStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests
{
  public class SomeData : EventBase<Guid, SomeDataAggregate>
  {
    public SomeData()
    {
    }
    public SomeData(Guid entityId)
    {
      EntityId = entityId;
    }

    protected override void ApplyInternal(SomeDataAggregate entity)
    {
    }
  }

  public class SomeDataAggregate : BaseAggregate<Guid>
  {

    public SomeDataAggregate(Guid entityId)
    {
      EntityId = entityId;
    }

    public SomeDataAggregate()
    {
    }
  }
}
