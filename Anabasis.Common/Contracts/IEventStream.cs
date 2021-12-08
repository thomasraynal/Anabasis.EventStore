using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IEventStream : IMessageQueue
    {
        IObservable<IEvent> OnEvent();
    }
}
