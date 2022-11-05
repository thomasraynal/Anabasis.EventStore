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
        Task<(bool isConnected, Exception? exception)> CheckConnectivity(CancellationToken cancellationToken = default);
        Task Emit(IEvent @event, SendEventOptions? sendEventOptions = null, CancellationToken cancellationToken = default);
        Task Emit(IEnumerable<IEvent> eventBatch, CreateBatchOptions? createBatchOptions = null, CancellationToken cancellationToken = default);
        Task UnSubscribeFromEventHub(Guid subscriptionId);
        Task<Guid> SubscribeToEventHub(Func<IMessage[],CancellationToken, Task> onEventsReceived);
        IObservable<(IMessage[] messages, CancellationToken cancellationToken)> SubscribeToEventHub();
    }
}