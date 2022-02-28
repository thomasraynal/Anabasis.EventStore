using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IDispatchQueue : IDisposable
    {
        bool CanEnqueue();
        void Enqueue(IMessage message);
    }
}
