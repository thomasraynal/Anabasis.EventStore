using System;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMQueue : IDisposable
    {
        string Id { get; }
        bool IsWiredUp { get; }
        IObservable<IEvent> OnEvent();
        void Connect();
    }
}
