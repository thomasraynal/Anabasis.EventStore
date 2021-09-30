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
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;

namespace Anabasis.EventStore
{
    public class EventTypeProviderFactory : IEventTypeProviderFactory
    {
        private readonly Dictionary<Type,IEventTypeProvider> _eventTypeProviders;

        public EventTypeProviderFactory()
        {
            _eventTypeProviders = new Dictionary<Type, IEventTypeProvider>();
        }

        public IEventTypeProvider Get(Type actorType)
        {
            return _eventTypeProviders[actorType];
        }

        public void Add<TActor>(IEventTypeProvider eventTypeProvider)
        {
            _eventTypeProviders.Add(typeof(TActor), eventTypeProvider);
        }
    }

    public interface IEventTypeProviderFactory
    {
        IEventTypeProvider Get(Type actorType);
        void Add<TActor>(IEventTypeProvider eventTypeProvider);
    }

    public class World
    {
        internal List<(Type actorType, IActorBuilder builder)> ActorBuilders { get; set; }

        private readonly IEventTypeProviderFactory _eventTypeProviderFactory;
        private readonly IServiceCollection _services;

        internal World(IServiceCollection services)
        {
            ActorBuilders = new List<(Type, IActorBuilder)>();
            _eventTypeProviderFactory = new EventTypeProviderFactory();
            _services = services;

            _services.AddSingleton(_eventTypeProviderFactory);
        }

        public StatelessActorBuilder<TActor> AddStatelessActor<TActor>(IEventTypeProvider eventTypeProvider = null) where TActor : class, IStatelessActor
        {

            eventTypeProvider ??= new ConsumerBasedEventProvider<TActor>();

            _eventTypeProviderFactory.Add<TActor>(eventTypeProvider);

            var statelessActorBuilder = new StatelessActorBuilder<TActor>(this);

            _services.AddSingleton<TActor>();

            return statelessActorBuilder;

        }

        internal void Add<TActor>(IActorBuilder actorBuilder) where TActor : IStatelessActor
        {
            ActorBuilders.Add((typeof(TActor), actorBuilder));
        }
    }

    public class StatelessActorBuilder<TActor> : IActorBuilder
        where TActor : IStatelessActor
    {
        private readonly World _world;
        private readonly List<Func<IConnectionStatusMonitor, IEventStoreQueue>> _queuesToRegisterTo;

        public StatelessActorBuilder(World world)
        {
            _world = world;
            _queuesToRegisterTo = new List<Func<IConnectionStatusMonitor, IEventStoreQueue>>();
        }

        public StatelessActorBuilder<TActor> WithSubscribeToAllQueue(IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndEventStoreQueue = new Func<IConnectionStatusMonitor, IEventStoreQueue>((connectionMonitor) =>
            {
                var subscribeFromEndEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration();

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndEventStoreQueue = new SubscribeFromEndEventStoreQueue(
                  connectionMonitor,
                  subscribeFromEndEventStoreQueueConfiguration,
                  eventProvider);

                return subscribeFromEndEventStoreQueue;

            });

            _queuesToRegisterTo.Add(getSubscribeFromEndEventStoreQueue);

            return this;

        }

        public World CreateActor()
        {
            _world.Add<TActor>(this);

            return _world;
        }


        public Func<IConnectionStatusMonitor, IEventStoreQueue>[] GetQueueFactories()
        {
            return _queuesToRegisterTo.ToArray();
        }
    }


    public static class ServiceBuilderActorExtensions
    {
        #region Actor

        private static World _world;

        public static IApplicationBuilder BuildWorld(this IApplicationBuilder applicationBuilder)
        {
            foreach (var (actorType, builder) in _world.ActorBuilders)
            {
                var actor = (IStatelessActor)applicationBuilder.ApplicationServices.GetService(actorType);
                var connectionStatusMonitor = applicationBuilder.ApplicationServices.GetService<IConnectionStatusMonitor>();

                foreach (var getQueue in builder.GetQueueFactories())
                {
                    actor.SubscribeTo(getQueue(connectionStatusMonitor));
                }
            }

            return applicationBuilder;
        }

        public static World CreateWorld(this IServiceCollection services,
            string eventStoreUrl,
            ConnectionSettings connectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {
            if (null != _world) throw new InvalidOperationException("A world already exist");

            var eventStoreConnection = EventStoreConnection.Create(connectionSettings, new Uri(eventStoreUrl));

            services.AddSingleton<IConnectionStatusMonitor, ConnectionStatusMonitor>();
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton(connectionSettings);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddTransient<IEventStoreRepository, EventStoreRepository>();

            return _world = new World(services);

        }

        #endregion

        public static IServiceCollection AddEventStoreCatchupCache<TKey, TAggregate>(this IServiceCollection services,
          ClusterVNode clusterVNode,
          UserCredentials userCredentials,
          ConnectionSettings connectionSettings,
          ILoggerFactory loggerFactory,
          Action<CatchupEventStoreCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
        where TAggregate : IAggregate<TKey>, new()
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, loggerFactory);

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
          ILoggerFactory loggerFactory,
          Action<SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
        where TAggregate : IAggregate<TKey>, new()
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, loggerFactory);

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
          ILoggerFactory loggerFactory,
          Action<SubscribeFromEndCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
        where TAggregate : IAggregate<TKey>, new()
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, loggerFactory);

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
