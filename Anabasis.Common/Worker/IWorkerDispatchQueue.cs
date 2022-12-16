using System;

namespace Anabasis.Common.Worker
{
    public interface IWorkerDispatchQueue : IDisposable
    {
        string Id { get; }
        long ProcessedMessagesCount { get; }
        bool IsFaulted { get; }
        Exception? LastError { get; }
        string Owner { get; }
        bool CanPush();
        void Push(IMessage message);
        IMessage[] TryPush(IMessage[] messages, out IMessage[] unProcessedMessages);
    }
}