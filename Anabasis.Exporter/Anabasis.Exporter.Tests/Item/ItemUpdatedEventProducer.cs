using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.EventStore;

namespace Anabasis.Tests.Demo
{
  public class ItemUpdatedEventProducer : IHostedService
    {
        private Random _rand;
        private CompositeDisposable _cleanUp;
        private IEventStoreRepository<Guid> _repository;
        private IEventStoreCache<Guid, Item> _cache;
        private ILogger<ItemUpdatedEventProducer> _logger;
        private bool _isConnected;

        public ItemUpdatedEventProducer(IConnectionStatusMonitor monitor, IEventStoreRepository<Guid> repository, IEventStoreCache<Guid, Item> cache, ILogger<ItemUpdatedEventProducer> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;

            var isConnected = monitor.IsConnected
                .ObserveOn(Scheduler.Default)
                .Subscribe(obs =>
            {
                _isConnected = obs;
            });

            _cleanUp = new CompositeDisposable(isConnected);

        }

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_rand.Next(s.Length)]).ToArray());
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _rand = new Random();


            var cleanUp1 = _cache.AsObservableCache()
                             .Connect()
                             .FilterEvents(item => item.State == ItemState.Ready)
                             .Subscribe( obs =>
                             {

                                 foreach (var item in obs)
                                 {

                                     if (_rand.Next(0, 4) == 1)
                                     {
                                         Scheduler.Default.Schedule(async () =>
                                         {
                                             await Task.Delay(_rand.Next(500, 1000));

                                             var itemDeletedEvent = new DeleteItemEvent();

                                              _repository.Apply(item.Current, itemDeletedEvent);

                                         });

                                     }

                                 }

                             });



            var cleanUp2 = _cache.AsObservableCache()
                          .Connect()
                          .FilterEvents(item => item.State == ItemState.Created)
                          .Subscribe( obs =>
                         {

                             foreach (var item in obs)
                             {
                                 Scheduler.Default.Schedule(async () =>
                                 {

                                     await Task.Delay(_rand.Next(500, 1500));

                                     var itemUpdatedEvent = new UpdateItemPayloadEvent()
                                     {
                                         Payload = RandomString(10)
                                     };

                                     //refacto: disconnected repository cache
                                     if (!_isConnected) return;

                                      _repository.Apply(item.Current, itemUpdatedEvent);


                                 });
                             }

                         });


            _cleanUp.Add(cleanUp1);
            _cleanUp.Add(cleanUp2);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cleanUp.Dispose();

            return Task.CompletedTask;
        }
    }
}
