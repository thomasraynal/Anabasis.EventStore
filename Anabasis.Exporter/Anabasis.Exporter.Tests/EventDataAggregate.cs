using Anabasis.EventStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Tests
{
  public class SomeData : EventBase<Guid, EventDataAggregate>
  {
    public SomeData()
    {
      EntityId = Guid.NewGuid();
    }

    public string Data { get; set; }

    protected override void ApplyInternal(EventDataAggregate entity)
    {
    }
  }

  public class EventDataAggregate : BaseAggregate<Guid>
  {
    public EventDataAggregate()
    {
      EntityId = Guid.NewGuid();
    }
  }
}
