﻿using Anabasis.Common;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples2
{

    public enum MerchantProductStatus
    {
        None,
        Created,
        Available,
        SoldOff,
        Removed
    }

    public class MerchantProductCreatedEvent : BaseAggregateEvent<MerchantProduct>
    {

        public MerchantProductCreatedEvent(string entityId, Guid? correlationId = null, Guid? causeId = null) : base(entityId, correlationId, causeId)
        {
        }

        public override void Apply([NotNull] MerchantProduct entity)
        {
            entity.MerchantProductStatus = MerchantProductStatus.Created;
        }
    }

    public class MerchantProductAvailableEvent : BaseAggregateEvent<MerchantProduct>
    {
        public int InitialQuantity { get; set; }

        public MerchantProductAvailableEvent(string entityId, int initialQuantity, Guid? correlationId = null, Guid? causeId = null) : base(entityId, correlationId, causeId)
        {
            InitialQuantity = initialQuantity;
        }

        public override void Apply([NotNull] MerchantProduct entity)
        {
            entity.MerchantProductStatus = MerchantProductStatus.Available;
            entity.Quantity = InitialQuantity;
        }
    }

    public class MerchantProductSoldOffEvent : BaseAggregateEvent<MerchantProduct>
    {

        public MerchantProductSoldOffEvent(string entityId, Guid? correlationId = null, Guid? causeId = null) : base(entityId, correlationId, causeId)
        {
        }

        public override void Apply([NotNull] MerchantProduct entity)
        {
            entity.MerchantProductStatus = MerchantProductStatus.SoldOff;
            entity.Quantity = 0;
        }
    }

    public class MerchantProductQuantityUpdatedEvent : BaseAggregateEvent<MerchantProduct>
    {
        public int CurrentQuantity { get; set; }

        public MerchantProductQuantityUpdatedEvent(string entityId, int currentQuantity, Guid? correlationId = null, Guid? causeId = null) : base(entityId, correlationId, causeId)
        {
            CurrentQuantity = currentQuantity;
        }

        public override void Apply([NotNull] MerchantProduct entity)
        {
            entity.MerchantProductStatus = MerchantProductStatus.SoldOff;
            entity.Quantity = CurrentQuantity;
        }
    }

    public class MerchantProductQuantityRemovedEvent : BaseAggregateEvent<MerchantProduct>
    {

        public MerchantProductQuantityRemovedEvent(string entityId, Guid? correlationId = null, Guid? causeId = null) : base(entityId, correlationId, causeId)
        {
        }

        public override void Apply([NotNull] MerchantProduct entity)
        {
            entity.MerchantProductStatus = MerchantProductStatus.Removed;
            entity.Quantity = 0;
        }
    }

    public class MerchantProduct : BaseAggregate
    {
        public MerchantProduct()
        {
        }

        public MerchantProduct(string productName, int initialQuantity, MerchantProductStatus merchantProductStatus)
        {
            EntityId = productName;
            Quantity = initialQuantity;
            MerchantProductStatus = merchantProductStatus;
        }

        public int Quantity { get; set; }
        public MerchantProductStatus MerchantProductStatus { get; set; }

    }

    public class MerchantCatalogActor : BaseEventStoreStatefulActor<MerchantProduct>
    {
        public MerchantCatalogActor(IActorConfiguration actorConfiguration, IAggregateCache<MerchantProduct> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreCache, loggerFactory)
        {
        }

        public MerchantCatalogActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ISnapshotStore<MerchantProduct> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, connectionStatusMonitor, snapshotStore, snapshotStrategy, loggerFactory)
        {
        }

    }

    public class Registry : ServiceRegistry
    {
        public Registry()
        {
            For<IEventStoreBus>().Use<EventStoreBus>().Singleton();
        }
    }

    internal class Program
    {
   

        static void Main(string[] args)
        {

            Task.Run(async () =>
            {

                var actorConfiguration = new ActorConfiguration();

                var userCredentials = new UserCredentials("admin", "changeit");

                var connectionSettings = ConnectionSettings.Create()
                    .UseDebugLogger()
                    .SetDefaultUserCredentials(userCredentials)
                    .KeepRetrying()
                    .Build();

                var clusterVNode = EmbeddedVNodeBuilder
                  .AsSingleNode()
                  .RunInMemory()
                  .RunProjections(ProjectionType.All)
                  .StartStandardProjections()
                  .WithWorkerThreads(1)
                  .Build();

                await clusterVNode.StartAsync(true);

                var defaultEventTypeProvider = new DefaultEventTypeProvider<MerchantProduct>(() => new[] {
                          typeof(MerchantProductAvailableEvent),
                          typeof(MerchantProductQuantityRemovedEvent),
                          typeof(MerchantProductQuantityUpdatedEvent),
                          typeof(MerchantProductSoldOffEvent),
                          typeof(MerchantProductCreatedEvent)
                      });

                var products = new[]{
                    "ProductA",
                    "ProductB",
                    "ProductC",
                    "ProductD",
                    "ProductE",
                    "ProductF"
                };

                var merchantCatalogActor = EventStoreEmbeddedStatefulActorBuilder<MerchantCatalogActor, MerchantProduct, Registry>
                                .Create(clusterVNode, connectionSettings, ActorConfiguration.Default)
                                .WithReadManyStreamsFromStartCache(streamIds: products, eventTypeProvider: defaultEventTypeProvider)
                                .WithBus<IEventStoreBus>()
                                .Build();
           

                await Task.Delay(5000);


                //*************

                var loadedProducts = merchantCatalogActor.State.GetCurrents();

                foreach (var product in products)
                {
                    if (!loadedProducts.Any(loadedProduct => loadedProduct.EntityId == product))
                    {
                        await merchantCatalogActor.EmitEventStore(new MerchantProductCreatedEvent(product));
                        await merchantCatalogActor.EmitEventStore(new MerchantProductAvailableEvent(product, 10));
                    }
                }

                await Task.Delay(500);

            });

            Console.Read();
        }
    }
}
