using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IDispatchQueue : IDisposable
    {
        string Owner { get; }
        string Id { get; }
        bool IsFaulted { get; }
        Exception? LastError { get; }
        bool CanEnqueue();
        void Enqueue(IMessage message);
    }
}
