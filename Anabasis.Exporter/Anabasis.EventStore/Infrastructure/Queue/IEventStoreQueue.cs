using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Queue
{
  public interface IEventStoreQueue: IDisposable
  {
    IObservable<IEvent> OnEvent();
  }
}
