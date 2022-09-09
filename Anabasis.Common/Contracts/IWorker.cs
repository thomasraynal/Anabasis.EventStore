using Anabasis.Common.Worker;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Contracts
{
    public interface IWorker : IHealthCheck, IDisposable
    {
        string Id { get; }
        bool IsConnected { get; }
        bool IsFaulted { get; }
        Exception? LastError { get; }   
        IWorkerDispatchQueue[] GetWorkerDispatchQueues();
        TBus GetConnectedBus<TBus>() where TBus : class;
        Task WaitUntilConnected(TimeSpan? timeout = null);
        void AddDisposable(IDisposable disposable);
        Task OnInitialized();
        Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false);
        Task Handle(IEvent[] messages);
        Task Handle(IMessage[] messages, TimeSpan? timeout = null);

    }
}
