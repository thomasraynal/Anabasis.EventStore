using System;

namespace Anabasis.Common
{
    public interface IEventStoreStream : IMessageQueue
    {
        IObservable<IMessage> OnMessage();
    }
}
