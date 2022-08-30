using System;

namespace Anabasis.Common.Worker
{
    public interface IWorkerDispatchQueue: IDisposable
    {
        string Id { get; }
        bool IsFaulted { get; }
        Exception? LastError { get; }
        string Owner { get; }
        bool CanPush();
        void Push(IMessage message);
    }
}