using Anabasis.Actor;
using Anabasis.EventStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests
{
  public class SomeMoreData : BaseEvent
  {
    public SomeMoreData(Guid correlationId, string streamId) : base(correlationId, streamId)
    {
    }

    public override string Log()
    {
      return nameof(SomeMoreData);
    }
  }

  public class SomeData<TKey> : BaseAggregateEvent<TKey, SomeDataAggregate<TKey>>
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
