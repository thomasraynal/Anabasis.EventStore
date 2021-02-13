using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.EventStore;

namespace Anabasis.Tests.Demo
{
    public class ItemCreatedEventProducer : IHostedService
    {
        private IDisposable _cleanup;
        private IEventStoreRepository<Guid> _repository;

        public ItemCreatedEventProducer(IEventStoreRepository<Guid> repository)
        {
            _repository = repository;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cleanup = Observable.Interval(TimeSpan.FromMilliseconds(500))
                                 .Subscribe( _ =>
                                 {
                                     var item = new Item();
                                     
                                      _repository.Apply(item, new CreateItemEvent());
                                 });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cleanup.Dispose();

            return Task.CompletedTask;
        }
    }
}
