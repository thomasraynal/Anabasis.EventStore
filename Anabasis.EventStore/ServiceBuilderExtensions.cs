using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Microsoft.Extensions.DependencyInjection;
using System;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Queue;

namespace Anabasis.EventStore
{
    public class World
    {
        public World(string eventStoreUrl, EventTypeProviderFactory eventTypeProviderFactory, 
            Func<ConnectionSettings> getConnectionSettings, 
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration,
            IServiceCollection serviceCollection)
        {
            EventTypeProviderFactory = eventTypeProviderFactory;
            GetConnectionSettings = getConnectionSettings;
            GetEventStoreRepositoryConfiguration = getEventStoreRepositoryConfiguration;
            EventStoreUrl = eventStoreUrl;
            ServiceCollection = serviceCollection;
        }

        internal IServiceCollection ServiceCollection { get; private set; }
        internal string EventStoreUrl { get; private set; }
        internal EventTypeProviderFactory EventTypeProviderFactory { get; private set; }
        internal Func<ConnectionSettings> GetConnectionSettings { get; private set; }
        internal Action<IEventStoreRepositoryConfiguration> GetEventStoreRepositoryConfiguration { get; private set; }

        public World AddStatelessActor<TActor>()
            where TActor : class, IStatelessActor
        {
            ServiceCollection.AddSingleton<TActor>();

            return this;
        }

        public World AddStatefullActor<TActor>()
            where TActor : class, IStatelessActor
        {


            return this;
        }

        //public World WithSubscribeToAllQueue(IEventTypeProvider eventTypeProvider = null)
        //{
        //    var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration();


        //    var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
        //      ConnectionMonitor,
        //      volatileEventStoreQueueConfiguration,
        //      eventProvider);

        //    _queuesToRegisterTo.Add(volatileEventStoreQueue);

        //    return this;
        //}

        //public World WithSubscribeToOneStreamQueue(string streamId, IEventTypeProvider eventTypeProvider = null)
        //{
        //    var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration(UserCredentials);

        //    var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

        //    var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
        //      ConnectionMonitor,
        //      volatileEventStoreQueueConfiguration,
        //      eventProvider);

        //    _queuesToRegisterTo.Add(volatileEventStoreQueue);

        //    return this;
        //}

        //public StatelessActorBuilder WithPersistentSubscriptionQueue(string streamId, string groupId)
        //{
        //    var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(streamId, groupId);

        //    var eventProvider = new ConsumerBasedEventProvider<TActor>();

        //    var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
        //      ConnectionMonitor,
        //      persistentEventStoreQueueConfiguration,
        //      eventProvider);

        //    _queuesToRegisterTo.Add(persistentSubscriptionEventStoreQueue);

        //    return this;
        //}

    }

    public static class ServiceBuilderExtensions
    {

        #region Actor

        private static World _actorServiceBuilder;

        public static World CreateWorld(this IServiceCollection services,
            string eventStoreUrl,
            Func<ConnectionSettings> getConnectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {
            if (null != _actorServiceBuilder) throw new InvalidOperationException("Builder already exists");

            var actorServiceBuilder = new World(eventStoreUrl,
                new EventTypeProviderFactory(),
                getConnectionSettings,
                getEventStoreRepositoryConfiguration,
                services);

            var getEventStoreConnection = new Func<IEventStoreConnection>(() => EventStoreConnection.Create(getConnectionSettings(), new Uri(eventStoreUrl)));

            services.AddSingleton(getEventStoreConnection);
            services.AddSingleton(getConnectionSettings);

            services.AddTransient<IConnectionStatusMonitor, ConnectionStatusMonitor>();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepository, EventStoreRepository>();

            _actorServiceBuilder = actorServiceBuilder;

            return actorServiceBuilder;
        }



        public static IServiceCollection AddStatelessActor<TActor>(this IServiceCollection services,
            string eventStoreUrl,
            Func<ConnectionSettings> getConnectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null,
            Func<TActor, IEventTypeProvider> getEventTypeProvider = null)
            where TActor : class, IStatelessActor
        {
            var eventStoreConnection = EventStoreConnection.Create(getConnectionSettings(), new Uri(eventStoreUrl));

            var getEventStoreConnection = new Func<IEventStoreConnection>(() => EventStoreConnection.Create(getConnectionSettings(), new Uri(eventStoreUrl)));

            services.AddSingleton(getEventStoreConnection);

            services.AddSingleton(getConnectionSettings);

            services.AddTransient<IConnectionStatusMonitor, ConnectionStatusMonitor>();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            getEventTypeProvider ??= (_) => new ConsumerBasedEventProvider<TActor>();

            services.AddSingleton(getEventTypeProvider);

            services.AddSingleton<IEventStoreRepository, EventStoreRepository>();

            services.AddSingleton<TActor>();

            return services;

        }


        #endregion

        public static IServiceCollection AddEventStoreCatchupCache<TKey, TAggregate>(this IServiceCollection services,
      ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      Action<CatchupEventStoreCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
        where TAggregate : IAggregate<TKey>, new()
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection);

            var catchupEventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TAggregate>(userCredentials);
            cacheBuilder?.Invoke(catchupEventStoreCacheConfiguration);

            services.AddTransient<IEventTypeProvider<TKey, TAggregate>, ServiceCollectionEventTypeProvider<TKey, TAggregate>>();
            services.AddSingleton(catchupEventStoreCacheConfiguration);
            services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
            services.AddTransient<CatchupEventStoreCache<TKey, TAggregate>>();

            return services;

        }

        public static IServiceCollection AddEventStoreSingleStreamCatchupCache<TKey, TAggregate>(this IServiceCollection services,
          ClusterVNode clusterVNode,
          UserCredentials userCredentials,
          ConnectionSettings connectionSettings,
          string streamId,
          Action<SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
        where TAggregate : IAggregate<TKey>, new()
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection);

            var singleStreamCatchupEventStoreCacheConfiguration = new SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>(streamId, userCredentials);
            cacheBuilder?.Invoke(singleStreamCatchupEventStoreCacheConfiguration);

            services.AddTransient<IEventTypeProvider, ServiceCollectionEventTypeProvider<TKey, TAggregate>>();
            services.AddSingleton(singleStreamCatchupEventStoreCacheConfiguration);
            services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
            services.AddTransient<SingleStreamCatchupEventStoreCache<TKey, TAggregate>>();

            return services;

        }

        public static IServiceCollection AddEventStoreVolatileCache<TKey, TAggregate>(this IServiceCollection services,
          ClusterVNode clusterVNode,
          UserCredentials userCredentials,
          ConnectionSettings connectionSettings,
          Action<SubscribeFromEndCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
        where TAggregate : IAggregate<TKey>, new()
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection);

            var volatileCacheConfiguration = new SubscribeFromEndCacheConfiguration<TKey, TAggregate>(userCredentials);
            cacheBuilder?.Invoke(volatileCacheConfiguration);

            services.AddTransient<IEventTypeProvider, ServiceCollectionEventTypeProvider<TKey, TAggregate>>();
            services.AddSingleton(volatileCacheConfiguration);
            services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
            services.AddTransient<SubscribeFromEndEventStoreCache<TKey, TAggregate>>();

            return services;

        }

        //  public static IServiceCollection AddEventStorePersistentSubscriptionCache<TKey, TAggregate>(this IServiceCollection services,
        //  ClusterVNode clusterVNode,
        //  UserCredentials userCredentials,
        //  ConnectionSettings connectionSettings,
        //  string streamId,
        //  string groupId,
        //  Action<PersistentSubscriptionCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
        //where TAggregate : IAggregate<TKey>, new()
        //  {

        //    var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
        //    var connectionMonitor = new ConnectionStatusMonitor(connection);

        //    var persistentSubscriptionCacheConfiguration = new PersistentSubscriptionCacheConfiguration<TKey, TAggregate>(streamId, groupId, userCredentials);
        //    cacheBuilder?.Invoke(persistentSubscriptionCacheConfiguration);

        //    services.AddTransient<IEventTypeProvider, ServiceCollectionEventTypeProvider<TKey, TAggregate>>();
        //    services.AddSingleton(persistentSubscriptionCacheConfiguration);
        //    services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
        //    services.AddTransient<PersistentSubscriptionEventStoreCache<TKey, TAggregate>>();

        //    return services;

        //  }

        #region Repository

        public static IServiceCollection AddEventStoreAggregateRepository<TKey, TRepository>(this IServiceCollection services,
          ClusterVNode clusterVNode,
          ConnectionSettings connectionSettings,
          Action<IEventStoreRepositoryConfiguration> repositoryBuilder = null) where TRepository : class, IEventStoreAggregateRepository<TKey>
        {
            var eventStoreConnection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            services.AddSingleton(eventStoreConnection);

            services.AddSingleton<IConnectionStatusMonitor, ConnectionStatusMonitor>();
            services.AddSingleton<IEventStoreAggregateRepository<TKey>, EventStoreAggregateRepository<TKey>>();
            services.AddTransient<IEventTypeProvider, ServiceCollectionEventTypeProvider<TKey>>();

            var configuration = new EventStoreRepositoryConfiguration();

            repositoryBuilder?.Invoke(configuration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(configuration);

            return services;
        }

        #endregion Repository

    }
}
