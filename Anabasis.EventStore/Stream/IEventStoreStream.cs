using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Stream
{
    public interface IEventStoreStream : IDisposable
    {
        string Id { get; }
        bool IsWiredUp { get; }
        IObservable<IEvent> OnEvent();
        void Connect();
    }
}
