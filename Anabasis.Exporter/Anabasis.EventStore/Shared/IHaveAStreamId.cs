using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Shared
{
  public interface IHaveAStreamId
  {
    string GetStreamName();
  }
}
