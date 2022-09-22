using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore2.Configuration;
using EventStore.ClientAPI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public class EventStoreBus : IEventStoreBus
    {
        public string BusId => throw new NotImplementedException();

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task EmitEventStore<TEvent>(TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent
        {
            throw new NotImplementedException();
        }

        public Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {
            throw new NotImplementedException();
        }

        public SubscribeToAllEventStoreStream SubscribeFromEndToAllStreams(Action<IMessage, TimeSpan?> onMessageReceived, IEventTypeProvider eventTypeProvider, Action<SubscribeToAllStreamsConfiguration> getSubscribeFromEndEventStoreStreamConfiguration = null)
        {
            throw new NotImplementedException();
        }

        public PersistentSubscriptionEventStoreStream SubscribeToPersistentSubscriptionStream(string streamId, string groupId, Action<IMessage, TimeSpan?> onMessageReceived, IEventTypeProvider eventTypeProvider, Action<PersistentSubscriptionStreamConfiguration> getPersistentSubscriptionEventStoreStreamConfiguration = null)
        {
            throw new NotImplementedException();
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }
    }
}
