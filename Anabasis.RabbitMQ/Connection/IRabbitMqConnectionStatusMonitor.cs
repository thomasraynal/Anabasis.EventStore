using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ.Connection
{
    public interface IRabbitMqConnectionStatusMonitor
    {
        bool IsConnected { get; }
        ConnectionInfo ConnectionInfo { get; }
        IObservable<bool> OnConnected { get; }
        RabbitMqConnection RabbitMqConnection { get; }
        IObservable<IConnected<RabbitMqConnection>> GetRabbitMqConnectionStatus();
    }
}
