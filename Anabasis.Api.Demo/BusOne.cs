using Anabasis.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Api.Demo
{
    public class DummyBusConnectionMonitor : IConnectionStatusMonitor
    {
        public bool IsConnected => true;

        public ConnectionInfo ConnectionInfo => ConnectionInfo.InitialConnected;

        public IObservable<bool> OnConnected => throw new NotImplementedException();

        public void Dispose()
        {

        }
    }

    public class BusOne : IBusOne
    {
        private readonly List<Action<IEvent>> _subscribers;

        public BusOne()
        {
            _subscribers = new List<Action<IEvent>>();
        }

        public string BusId { get; }

        public IConnectionStatusMonitor ConnectionStatusMonitor => new DummyBusConnectionMonitor();

        public void Push(IEvent @event)
        {
            foreach (var subscribers in _subscribers)
            {
                subscribers(@event);
            }
        }
        public void Subscribe(Action<IEvent> onEventReceived)
        {
            _subscribers.Add(onEventReceived);
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy("healthcheck from BusOne", new Dictionary<string, object>()
            {
                {"BusOne", "ok"}
            }));
        }

        public void Dispose()
        {

        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            return Task.CompletedTask;
        }
    }
}
