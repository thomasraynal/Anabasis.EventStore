using Anabasis.EventStore;
using System;

namespace Anabasis.Tests.Components
{
  public class SomeRandomEvent : IEntityEvent<string>
  {
    public SomeRandomEvent()
    {
      Name = nameof(SomeRandomEvent);
    }

    public string Name { get; set; }
    public string EntityId { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public string GetStreamName()
    {
      return EntityId;
    }
  }
}
