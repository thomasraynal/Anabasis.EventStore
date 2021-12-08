using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ._Temp
{
    public interface IActor
    {
        string Id { get; }
        bool IsConnected { get; }
        Task WaitUntilConnected(TimeSpan? timeout = null);
    }
}
