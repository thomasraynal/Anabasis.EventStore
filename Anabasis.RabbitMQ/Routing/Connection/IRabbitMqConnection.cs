using RabbitMQ.Client;
using System;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqConnection: IDisposable
    {
        bool IsBlocked { get; }
        void DoWithChannel(Action<IModel> action);
        T DoWithChannel<T>(Func<IModel, T> function);
        IBasicProperties GetBasicProperties();
    }
}