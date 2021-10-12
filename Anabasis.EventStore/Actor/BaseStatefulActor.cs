using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseStatefulActor<TKey, TAggregate> : BaseStatelessActor, IDisposable, IStatefulActor<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {

        public BaseStatefulActor(IEventStoreAggregateRepository<TKey> eventStoreRepository, IEventStoreCache<TKey, TAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
        {
            _eventStoreAggregateRepository = eventStoreRepository;

            State = eventStoreCache;
        }


        public BaseStatefulActor(IEventStoreAggregateRepository<TKey> eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory =null) : base(eventStoreRepository, loggerFactory)
        {
            _eventStoreAggregateRepository = eventStoreRepository;

            var getEventStoreCache = eventStoreCacheFactory.Get<TKey, TAggregate>(GetType());

            State = getEventStoreCache(connectionStatusMonitor);

        }

        private readonly IEventStoreAggregateRepository<TKey> _eventStoreAggregateRepository;

        public IEventStoreCache<TKey, TAggregate> State { get; internal set; }

        public async Task EmitEntityEvent<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEntity<TKey>
        {
            if (!_eventStoreAggregateRepository.IsConnected) throw new InvalidOperationException("Not connected");

            await _eventStoreAggregateRepository.Emit(@event, extraHeaders);
        }

    }
}
