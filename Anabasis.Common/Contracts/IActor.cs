using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IActor : IDisposable
    {
        string Id { get; }
        bool IsConnected { get; }
        Task<IAnabasisHealthCheck[]> GetHealthChecks();
        TBus GetConnectedBus<TBus>() where TBus : class;
        Task WaitUntilConnected(TimeSpan? timeout = null);
        Task OnEventReceived(IEvent @event, TimeSpan? timeout = null);
        void AddDisposable(IDisposable disposable);
        void ConnectTo(IBus bus, bool closeUnderlyingSubscriptionOnDispose = false);
    }
}
