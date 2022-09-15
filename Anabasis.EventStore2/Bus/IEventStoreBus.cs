using Anabasis.Common;
using Anabasis.EventStore.Stream;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public interface IEventStoreBus: IBus
    {
        Task EmitEventStore<TEvent>(TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent;
        Task<TCommandResult> SendEventStore<TCommandResult>(ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse;
        void SubscribeToEventStream(IEventStoreStream eventStoreStream, Action<IMessage,TimeSpan?> onMessageReceived, bool closeSubscriptionOnDispose = false);

        SubscribeFromStartOrLaterToOneStreamEventStoreStream SubscribeFromStartToOneStream(
            string streamId,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration>? getSubscribeFromEndToOneStreamEventStoreStreamConfiguration = null);

        SubscribeFromEndToOneStreamEventStoreStream SubscribeFromEndToOneStream(
            string streamId,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration>? getSubscribeFromEndToOneStreamEventStoreStreamConfiguration = null);

        SubscribeToAllEventStoreStream SubscribeFromEndToAllStreams(
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<SubscribeToAllStreamsConfiguration>? getSubscribeFromEndEventStoreStreamConfiguration = null);

        PersistentSubscriptionEventStoreStream SubscribeToPersistentSubscriptionStream(
            string streamId,
            string groupId,
            Action<IMessage, TimeSpan?> onMessageReceived,
            IEventTypeProvider eventTypeProvider,
            Action<PersistentSubscriptionStreamConfiguration>? getPersistentSubscriptionEventStoreStreamConfiguration = null);

    }
}