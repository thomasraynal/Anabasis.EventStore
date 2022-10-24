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
    public class BusOne : IBus
    {
        public BusOne()
        {
        }

        public string BusId { get; }

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public void Push(IEvent push)
        {

        }
        public void Subscribe(Action<IMessage> onMessageReceived)
        {

        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }
    }
}
