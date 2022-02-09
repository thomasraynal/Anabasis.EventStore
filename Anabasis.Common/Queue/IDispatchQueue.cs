using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IDispatchQueue<TMessage> : IDisposable
    {
        bool CanEnqueue();
        void Enqueue(TMessage message);
    }
}
