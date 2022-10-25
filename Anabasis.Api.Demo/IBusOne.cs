using Anabasis.Common;
using System;

namespace Anabasis.Api.Demo
{
    public interface IBusOne: IBus
    {
        void Push(IEvent @event);
        void Subscribe(Action<IEvent> onEventReceived);
    }
}