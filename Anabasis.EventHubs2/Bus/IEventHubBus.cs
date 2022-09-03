using Anabasis.Common;
using Azure.Messaging.EventHubs.Producer;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    public interface IEventHubBus: IBus, IDisposable
    {
        Task Emit(IEvent @event, SendEventOptions? sendEventOptions = null, CancellationToken cancellationToken = default);
        Task Emit(IEnumerable<IEvent> eventBatch, CreateBatchOptions? createBatchOptions = null, CancellationToken cancellationToken = default);
        void UnSubscribeToEventHub(Guid subscriptionId);
        Guid SubscribeToEventHub(Func<IEvent[]> onEventsReceived)
    }
}