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
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;

namespace Anabasis.Tests.Demo
{
    public class BlobEventProducerConsumer : IHostedService
    {
        private Random _rand;
        private CompositeDisposable _cleanup;
        private ILogger<BlobEventProducerConsumer> _logger;
        private IEventStoreAggregateRepository<Guid> _repository;
        private IEventStoreCache<Guid, Blob> _cache;

        private string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[_rand.Next(s.Length)]).ToArray());
        }

        public BlobEventProducerConsumer(IEventStoreAggregateRepository<Guid> repository, IEventStoreCache<Guid, Blob> cache, ILogger<BlobEventProducerConsumer> logger)
        {
            _repository = repository;
            _cache = cache;
            _cleanup = new CompositeDisposable();
            _logger = logger;
            _rand = new Random();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _rand = new Random();


            var cache = _cache.AsObservableCache();

            var creation = Observable.Interval(TimeSpan.FromMilliseconds(500))
                     .Subscribe( _ =>
                     {
                         var blob = new Blob()
                         {
                             Payload = RandomString(10)
                         };

                          _repository.Apply(blob, new FillBlobEvent());

                     });


            var update = cache
                  .Connect()
                  .FilterEvents(blob => blob.State == BlobState.Filled)
                  .Subscribe(changes =>
                 {

                     foreach (var blob in changes)
                     {
                         Scheduler.Default.Schedule(async () =>
                         {

                             await Task.Delay(_rand.Next(500, 5000));

                             var blobBurstedEvent = new BurstedBlobEvent();

                              _repository.Apply(blob.Current, blobBurstedEvent);

                         });
                     }

                 });

            var observation = cache
                .Connect()
                  .Subscribe(obs =>
                  {

                      foreach (var blob in obs)
                      {
                          _logger?.LogInformation($"{blob.Current}");
                      }

                  });

            _cleanup.Add(creation);
            _cleanup.Add(observation);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cleanup.Dispose();

            return Task.CompletedTask;
        }
    }
}
