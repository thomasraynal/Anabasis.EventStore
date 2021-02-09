using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Actor
{
  public class InMemoryInstanceAttribute : Attribute
  {
    public InMemoryInstanceAttribute(int instanceCount)
    {
      InstanceCount = instanceCount;
    }

    public int InstanceCount { get; }
  }
}
