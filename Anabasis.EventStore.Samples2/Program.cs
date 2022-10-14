using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Standalone.Embedded;
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

    public class MerchantCatalogActor : SubscribeToManyStreamsEventStoreStatefulActor<MerchantProduct>
    {
        public MerchantCatalogActor(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<MerchantProduct> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public MerchantCatalogActor(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, MultipleStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<MerchantProduct> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
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

                var newProducts = new[]{
                    "NEW-ProductW",
                    "NEW-ProductZ"
                };

                var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration(products);

                var merchantCatalogActor = EventStoreEmbeddedStatefulActorBuilder<MerchantCatalogActor, MultipleStreamsCatchupCacheConfiguration, MerchantProduct, Registry>
                                .Create(clusterVNode, connectionSettings, aggregateCacheConfiguration:  multipleStreamsCatchupCacheConfiguration, eventTypeProvider: defaultEventTypeProvider)
                                .WithBus<IEventStoreBus>()
                                .Build();

                await merchantCatalogActor.ConnectToEventStream();

                merchantCatalogActor.AsObservableCache().Connect().Subscribe((changeSet) =>
                {
                    foreach (var set in changeSet)
                    {
                        Console.WriteLine($"{set.Current.EntityId}=>{set.Reason}");
                    }
                });

                await Task.Delay(2000);


                var loadedProducts = merchantCatalogActor.GetCurrents();

                foreach (var product in products)
                {
                    if (!loadedProducts.Any(loadedProduct => loadedProduct.EntityId == product))
                    {
                        await merchantCatalogActor.EmitEventStore(new MerchantProductCreatedEvent(product));
                        await merchantCatalogActor.EmitEventStore(new MerchantProductAvailableEvent(product, 10));
                    }
                }

                await Task.Delay(500);

                await merchantCatalogActor.AddEventStoreStreams(newProducts);

                foreach (var newProduct in newProducts)
                {
                    await merchantCatalogActor.EmitEventStore(new MerchantProductCreatedEvent(newProduct));
                    await merchantCatalogActor.EmitEventStore(new MerchantProductAvailableEvent(newProduct, 10));
                }

                await Task.Delay(500);

         

            });

            Console.Read();
        }
    }
}
