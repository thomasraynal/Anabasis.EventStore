using Anabasis.Common;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore2.Configuration;
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
            Action<IMessage> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<PersistentSubscriptionStreamConfiguration>? getPersistentSubscriptionEventStoreStreamConfiguration = null);

        IDisposable SubscribeToOneOrManyStreams(
            string[] streamIds,
            Action<IMessage> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneOrManyStreamsConfiguration>? getSubscribeToOneOrManyStreamsConfiguration = null);

    }
}