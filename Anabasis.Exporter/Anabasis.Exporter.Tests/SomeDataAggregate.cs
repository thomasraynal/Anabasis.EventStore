using Anabasis.EventStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests
{
  public class SomeData<TKey> : EventBase<TKey, SomeDataAggregate<TKey>>
  {
    public SomeData()
    {
    }
    public SomeData(TKey entityId)
    {
      EntityId = entityId;
    }

    protected override void ApplyInternal(SomeDataAggregate<TKey> entity)
    {
    }
  }

  public class SomeDataAggregate<TKey> : BaseAggregate<TKey>
  {

    public SomeDataAggregate(TKey entityId)
    {
      EntityId = entityId;
    }

    public SomeDataAggregate()
    {
    }
  }
}
