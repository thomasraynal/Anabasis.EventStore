using Anabasis.RabbitMQ.Demo;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo2
{
    public class ChangeProductHostedService : IHostedService
    {
        private readonly IRabbitMqBus _rabbitMqBus;
        private readonly ProductInventoryActor _productInventoryActor;
        private readonly ILogger<ChangeProductHostedService> _logger;
        private readonly Random _rand;
        private IDisposable _disposable;

        public ChangeProductHostedService(IRabbitMqBus rabbitMqBus, ILogger<ChangeProductHostedService> logger, ProductInventoryActor productInventoryActor)
        {
            _rabbitMqBus = rabbitMqBus;
            _productInventoryActor = productInventoryActor;
            _logger = logger;
            _rand = new Random();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _disposable = Observable.Interval(TimeSpan.FromSeconds(1)).Subscribe((_) =>
              {
                  foreach (var product in StaticData.ProductIds)
                  {
                   
                      _rabbitMqBus.Emit(new ProductInventoryChanged(Guid.NewGuid(), Guid.NewGuid())
                      {
                          ProductId = product,
                          CurrentInventory = _rand.Next(0, 100)

                      }, StaticData.ProducyInventoryExchange);

                  }

              });

            _productInventoryActor.AsObservableCache().Connect().Subscribe(changetSet =>
            {

                foreach(var change in changetSet)
                {
                    _logger.LogInformation($"{change.Reason}=> {change.Current.EntityId}-{change.Current.Quantity} - v.{change.Current.Version}");
                }

            });

            return Task.CompletedTask;

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _disposable.Dispose();

            return Task.CompletedTask;
        }
    }
}
