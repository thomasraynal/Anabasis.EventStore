using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IActor : IHealthCheck, IDisposable
    {
        string Id { get; }
        bool IsConnected { get; }
        bool IsCaughtUp { get; }
        bool IsFaulted { get; }
        Exception? LastError { get; }
        TBus GetConnectedBus<TBus>() where TBus : class;
        Task WaitUntilConnected(TimeSpan? timeout = null);
        void OnMessageReceived(IMessage @event, TimeSpan? timeout = null);
        void AddToCleanup(IDisposable disposable);
        Task OnInitialized();
        Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false);
    }
}
