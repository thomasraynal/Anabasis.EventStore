using DynamicData;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.EventStore;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Shared;

namespace Anabasis.Tests.Demo
{
    public class ItemCount
    {
        public int Count { get; }
    }

    public class ItemEventConsumer : IHostedService
    {
        private CompositeDisposable _cleanup;
        private IEventStoreCache<Guid, Item> _cache;
        private ILogger<ItemEventConsumer> _logger;
     

        public ItemEventConsumer(IEventStoreCache<Guid, Item> cache, ILogger<ItemEventConsumer> logger)
        {
            _cache = cache;
            _logger = logger;
           
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var cache = _cache.AsObservableCache();

            var cleanup1 = cache.Connect()
                  .Subscribe(obs =>
                 {
                     foreach (var item in obs)
                     {
                       _logger?.LogInformation($"{item.Reason} - {item.Current.ToString()}");
                     }

                 });

            var cleanup2 = cache.Connect()
                .ToCollection()
                .Scan(GlobalItemState.Default, (previous, obs) =>
                 {
                     var created = obs.Where(c => c.State == ItemState.Created);
                     var deleted = obs.Where(c => c.State == ItemState.Deleted);
                     var ready = obs.Where(c => c.State == ItemState.Ready);

                     return new GlobalItemState()
                     {
                         Created = created != null ? created.Count() : 0,
                         Deleted = deleted != null ? deleted.Count() : 0,
                         Ready = ready != null ? ready.Count() : 0
                     };

                 })
                  .Subscribe(state =>
                  {
                      _logger?.LogInformation(state.ToString());
                  }
                );
     

            var cleanup3 = cache.Connect()  
                .FilterEvents(item=> item.State ==  ItemState.Deleted)
                .Buffer(2, 1)
                .Subscribe(changed =>
                {
                    var isOutStandingDeletion = changed.Count() > 1;
                    if (isOutStandingDeletion)
                    {
                       _logger?.LogInformation("That is a lot of deletion!");
                    }
                });



            _cleanup = new CompositeDisposable(cleanup1, cleanup2, cleanup3);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cleanup.Dispose();

            return Task.CompletedTask;
        }
    }
}
