using Anabasis.Common;
using Anabasis.EventStore.Bus;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Stream.Configuration;
using Anabasis.EventStore2.Configuration;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public interface IEventStoreBus : IBus
    {
        Task EmitEventStore<TEvent>(TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent;

        IDisposable SubscribeToPersistentSubscriptionStream(
            string streamId,
            string groupId,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<PersistentSubscriptionStreamConfiguration>? getPersistentSubscriptionEventStoreStreamConfiguration = null);

        IDisposable SubscribeToManyStreams(
            StreamIdAndPosition[] streamIds,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToManyStreamsConfiguration>? getSubscribeToManyStreamsConfiguration = null);

        IDisposable SubscribeToAllStreams(
            Position position,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToAllStreamsConfiguration>? getSubscribeToAllStreamsConfiguration = null);

        IDisposable SubscribeToOneStream(string streamId,
            long streamPosition,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneStreamConfiguration>? getSubscribeToOneStreamConfiguration = null);

    }
}