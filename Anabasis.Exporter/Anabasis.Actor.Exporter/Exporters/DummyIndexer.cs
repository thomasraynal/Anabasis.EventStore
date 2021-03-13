using Anabasis.Actor;
using Anabasis.EventStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class DummyIndexer : BaseActor
  {
    public DummyIndexer(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
    }
  }
}
