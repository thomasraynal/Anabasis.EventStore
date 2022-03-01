using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IMessageQueue : IDisposable
    {
        string Id { get; }
        void Connect();
        void Disconnect();
    }
}
