using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Shared
{
    public interface IConnected<out T>
    {
        T Value { get; }
        bool IsConnected { get; }
    }
}
