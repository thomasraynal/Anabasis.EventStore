using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{
    public interface IActor : IDisposable
    {
        Guid Id { get; }
    }
}
