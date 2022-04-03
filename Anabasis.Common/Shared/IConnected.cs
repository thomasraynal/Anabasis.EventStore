using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IConnected<out T>
    {
        T? Value { get; }
        bool IsConnected { get; }
    }
}
