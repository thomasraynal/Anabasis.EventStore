using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseOneOrManyStreamCatchupCache<TKey, TAggregate> : BaseCatchupCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        protected BaseOneOrManyStreamCatchupCache(IConnectionStatusMonitor connectionMonitor, MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> catchupCacheConfiguration, IEventTypeProvider<TKey, TAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<TKey, TAggregate> snapshotStore = null, ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            var catchupCacheSubscriptionHolders = catchupCacheConfiguration.StreamIds.Select(streamId => new CatchupCacheSubscriptionHolder<TKey, TAggregate>(streamId)).ToArray();

            Initialize(catchupCacheSubscriptionHolders);

        }
    }
}
