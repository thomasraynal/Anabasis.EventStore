using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Queue
{
    public interface IEventStoreQueue : IDisposable
    {
        IObservable<IEvent> OnEvent();
        void Connect();
    }
}
