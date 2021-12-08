using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IMessageQueue : IDisposable
    {
        string Id { get; }
        bool IsWiredUp { get; }
        void Connect();
    }
}
