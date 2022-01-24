using Anabasis.Common.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IBus : IDisposable
    {
        bool IsConnected { get; }
        void DoHealthCheck(bool shouldThrow = false);
    }
}
