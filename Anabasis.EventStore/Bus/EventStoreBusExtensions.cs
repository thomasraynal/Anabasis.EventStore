using Anabasis.Common;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Stream.Configuration;
using Anabasis.EventStore2.Configuration;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public static class EventStoreBusExtensions
    {

        public static async Task EmitEventStore<TEvent>(this IActor actor, TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent
        {
            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            await eventStoreBus.EmitEventStore(@event, timeout, extraHeaders);
        }

        public static void SubscribeToManyStreams(
            this IActor actor,
            string[] streamIds,
            Action<SubscribeToManyStreamsConfiguration>? getSubscribeToManyStreamsConfiguration = null,
            IEventTypeProvider? eventTypeProvider = null)
        {
            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider(actor.GetType());

            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            var subscribeFromEndEventStoreStream = eventStoreBus.SubscribeToManyStreams(
                streamIds,
                actor.OnMessageReceived,
                eventProvider,
                getSubscribeToManyStreamsConfiguration);

            actor.AddToCleanup(subscribeFromEndEventStoreStream);
        }

        public static void SubscribeToAllStreams(
            this IActor actor,
            Position position,
            Action<SubscribeToAllStreamsConfiguration>? getSubscribeToAllStreamsConfiguration = null,
            IEventTypeProvider? eventTypeProvider = null)
        {
            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider(actor.GetType());

            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            var subscribeFromEndEventStoreStream = eventStoreBus.SubscribeToAllStreams(
                position,
                actor.OnMessageReceived,
                eventProvider,
                getSubscribeToAllStreamsConfiguration);

            actor.AddToCleanup(subscribeFromEndEventStoreStream);
        }

        public static void SubscribeToPersistentSubscriptionStream(
            this IActor actor,
            string streamId,
            string groupId,
            IEventTypeProvider? eventTypeProvider = null,
            Action<PersistentSubscriptionStreamConfiguration>? getPersistentSubscriptionEventStoreStreamConfiguration = null)
        {
            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider(actor.GetType());

            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            var persistentSubscriptionEventStoreStream = eventStoreBus.SubscribeToPersistentSubscriptionStream(
                streamId,
                groupId,
                actor.OnMessageReceived,
                eventProvider,
                getPersistentSubscriptionEventStoreStreamConfiguration);

            actor.AddToCleanup(persistentSubscriptionEventStoreStream);
        }

    }
}
