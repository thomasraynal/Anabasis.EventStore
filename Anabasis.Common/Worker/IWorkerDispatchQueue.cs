using System;

namespace Anabasis.Common.Worker
{
    public interface IWorkerDispatchQueue: IAsyncDisposable
    {
        string Id { get; }
        bool IsFaulted { get; }
        Exception? LastError { get; }
        string Owner { get; }
        bool CanEnqueue();
        void Enqueue(IMessage message);
    }
}