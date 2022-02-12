using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IActor : IHealthCheck, IDisposable
    {
        string Id { get; }
        bool IsConnected { get; }
        TBus GetConnectedBus<TBus>() where TBus : class;
        Task WaitUntilConnected(TimeSpan? timeout = null);
        void OnEventReceived(IEvent @event, TimeSpan? timeout = null);
        void AddDisposable(IDisposable disposable);
        Task ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false);
    }
}
