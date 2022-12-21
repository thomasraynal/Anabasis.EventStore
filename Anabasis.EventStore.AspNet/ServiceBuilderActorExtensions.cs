using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Anabasis.Common.HealthChecks;
using Anabasis.Common;
using Anabasis.EventStore.AspNet.Builders;
using Anabasis.Common.Contracts;

namespace Anabasis.EventStore.AspNet
{

    public static class ServiceBuilderActorExtensions
    {

        public static IApplicationBuilder UseWorld(this IApplicationBuilder applicationBuilder)
        {
            var registerWorkerHealthChecksAndBus = new Action<IWorkerBuilder, Type>((builder, workerType) =>
            {
                var worker = (IWorker)applicationBuilder.ApplicationServices.GetService(workerType);

                if (null == worker)
                {
                    throw new NullReferenceException($"Worker {workerType} is not registered");
                }

                var healthCheckService = applicationBuilder.ApplicationServices.GetService<IDynamicHealthCheckProvider>();
                healthCheckService.AddHealthCheck(new HealthCheckRegistration(worker.Id, worker, HealthStatus.Unhealthy, null));

                foreach (var (_, factory) in builder.GetBusFactories())
                {
                    factory(applicationBuilder.ApplicationServices, worker);
                }

            });

            var initializeWorker = new Action<IWorkerBuilder, Type>((builder, workerType) =>
           {
               var worker = (IWorker)applicationBuilder.ApplicationServices.GetService(workerType);

               worker.OnInitialized().Wait();

           });


            var registerActorHealthChecksAndBus = new Action<IActorBuilder, Type>((builder, actorType) =>
            {
                var actor = (IAnabasisActor)applicationBuilder.ApplicationServices.GetService(actorType);

                if (null == actor)
                {
                    throw new NullReferenceException($"Actor {actorType} is not registred");
                }

                var healthCheckService = applicationBuilder.ApplicationServices.GetService<IDynamicHealthCheckProvider>();
                healthCheckService.AddHealthCheck(new HealthCheckRegistration(actor.Id, actor, HealthStatus.Unhealthy, null));

                foreach (var busConfiguration in builder.GetBusFactories())
                {
                    busConfiguration.factory(applicationBuilder.ApplicationServices, actor);
                }

            });

            var initializeActor = new Action<IActorBuilder, Type>((builder, actorType) =>
            {
                var actor = (IAnabasisActor)applicationBuilder.ApplicationServices.GetService(actorType);

                actor.OnInitialized().Wait();

            });

            var connectionStatusMonitor = applicationBuilder.ApplicationServices.GetService<IConnectionStatusMonitor<IEventStoreConnection>>();

            var world = applicationBuilder.ApplicationServices.GetService<World>();

            foreach (var (actorType, builder) in world.GetActorBuilders())
            {
                registerActorHealthChecksAndBus(builder, actorType);
                initializeActor(builder, actorType);
            }

            foreach (var (workerType, builder) in world.GetWorkerBuilders())
            {
                registerWorkerHealthChecksAndBus(builder, workerType);
                initializeWorker(builder, workerType);
            }

            return applicationBuilder;
        }

        public static World AddWorld(this IServiceCollection services)
        {
            var world = new World(services, false);

            services.AddSingleton(world);

            return world;

        }

        public static World AddWorld(this IServiceCollection services,
            string eventStoreConnectionString,
            ConnectionSettingsBuilder connectionSettingsBuilder,
            Action<IEventStoreRepositoryConfiguration>? getEventStoreRepositoryConfiguration = null)
        {

            var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString, connectionSettingsBuilder);

            var connectionSettings = connectionSettingsBuilder.Build();

            services.AddSingleton<IConnectionStatusMonitor<IEventStoreConnection>, EventStoreConnectionStatusMonitor>();
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton(connectionSettings);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddTransient<IEventStoreRepository, EventStoreRepository>();

            var world = new World(services, true);

            services.AddSingleton(world);

            return world;
        }

        public static World AddWorld(this IServiceCollection services,
            Uri eventStoreUrl,
            ConnectionSettings connectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var eventStoreConnection = EventStoreConnection.Create(connectionSettings, eventStoreUrl);

            services.AddSingleton<IConnectionStatusMonitor<IEventStoreConnection>, EventStoreConnectionStatusMonitor>();
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton(connectionSettings);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddTransient<IEventStoreRepository, EventStoreRepository>();

            var world = new World(services, true);

            services.AddSingleton(world);

            return world;

        }

    }
}
