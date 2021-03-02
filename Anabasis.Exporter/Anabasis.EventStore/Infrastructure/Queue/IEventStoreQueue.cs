using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Queue
{
  public interface IEventStoreQueue
  {
    IObservable<IEvent> OnEvent();
  }
}
