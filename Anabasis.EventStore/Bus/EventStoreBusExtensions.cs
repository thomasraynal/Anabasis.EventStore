using Anabasis.Common;
using Anabasis.EventStore.Stream;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public static class EventStoreBusExtensions
    {
        public static void SubscribeToEventStream(this IActor actor, IEventStoreStream eventStoreStream, bool closeSubscriptionOnDispose = false)
        {
            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            eventStoreBus.SubscribeToEventStream(eventStoreStream, actor.OnMessageReceived, closeSubscriptionOnDispose);
        }

        public static async Task EmitEventStore<TEvent>(this IActor actor, TEvent @event, TimeSpan? timeout = null, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEvent
        {
            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            await eventStoreBus.EmitEventStore(@event, timeout, extraHeaders);
        }

        public static async Task<TCommandResult> SendEventStore<TCommandResult>(this IActor actor, ICommand command, TimeSpan? timeout = null) where TCommandResult : ICommandResponse
        {
            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            return await eventStoreBus.SendEventStore<TCommandResult>(command, timeout);
        }

        public static void SubscribeFromEndToAllStreams(
            this IActor actor,
            Action<SubscribeFromEndEventStoreStreamConfiguration>? getSubscribeFromEndEventStoreStreamConfiguration = null,
            IEventTypeProvider? eventTypeProvider = null)
        {
            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider(actor.GetType());

            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            var subscribeFromEndEventStoreStream = eventStoreBus.SubscribeFromEndToAllStreams(
                actor.OnMessageReceived,
                eventProvider,
                getSubscribeFromEndEventStoreStreamConfiguration);

            actor.AddToCleanup(subscribeFromEndEventStoreStream);
        }

        public static void SubscribeToPersistentSubscriptionStream(
            this IActor actor,
            string streamId,
            string groupId,
            IEventTypeProvider? eventTypeProvider=null,
            Action<PersistentSubscriptionEventStoreStreamConfiguration>? getPersistentSubscriptionEventStoreStreamConfiguration = null)
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

        public static void SubscribeFromStartToOneStream(
            this IActor actor,
            string streamId,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration>? subscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration = null,
            IEventTypeProvider? eventTypeProvider = null)
        {
            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider(actor.GetType());

            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            var subscribeFromStartOrLaterToOneStreamEventStoreStream = eventStoreBus.SubscribeFromStartToOneStream(
                streamId,
                actor.OnMessageReceived,
                eventProvider,
                subscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration);

            actor.AddToCleanup(subscribeFromStartOrLaterToOneStreamEventStoreStream);
        }

        public static void SubscribeFromEndToOneStream(
        this IActor actor,
        string streamId,
        Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration>? subscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration = null,
        IEventTypeProvider? eventTypeProvider = null)
        {
            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider(actor.GetType());

            var eventStoreBus = actor.GetConnectedBus<IEventStoreBus>();

            var subscribeFromEndToOneStreamEventStoreStream = eventStoreBus.SubscribeFromEndToOneStream(
                streamId,
                actor.OnMessageReceived,
                eventProvider,
                subscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration);

            actor.AddToCleanup(subscribeFromEndToOneStreamEventStoreStream);
        }
    }
}
