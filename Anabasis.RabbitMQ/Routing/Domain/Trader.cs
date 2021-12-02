using RabbitMQ.Client;
using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing.Domain
{
    public class Trader : ISubscriber, IDisposable
    {
        public List<CurrencyPair> CurrencyPairs { get; }

        private readonly IBus _bus;
        private readonly ITraderConfiguration _configuration;

        public Trader(ITraderConfiguration traderConfiguration, IBusConfiguration busConfiguration, IConnection connection)
        {
            CurrencyPairs = new List<CurrencyPair>();

            var container = BusFactory.CreateContainer<RegistryForTests>(connection, busConfiguration);

            _bus = container.GetInstance<IBus>();

            _configuration = traderConfiguration;

            _bus.Subscribe(new EventSubscription<PriceChangedEvent>(_configuration.EventExchange, _configuration.RoutingStrategy, (@event) =>
            {
                var ccyPair = CurrencyPairs.FirstOrDefault(ccy => ccy.Id == @event.AggregateId);

                if (null == ccyPair)
                {
                    ccyPair = new CurrencyPair(@event.AggregateId);
                    CurrencyPairs.Add(ccyPair);
                }

                ccyPair.Ask = @event.Ask;
                ccyPair.Bid = @event.Bid;

                ccyPair.AppliedEvents.Add(@event);

            }));


        }

        public void Emit(IEvent @event)
        {
            _bus.Emit(@event, _configuration.EventExchange);
        }

        public async Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : ICommandResult
        {
            return await _bus.Send<TCommandResult>(command);
        }

        public void Dispose()
        {
            _bus.Dispose();
        }

        public void Subscribe<TEvent>(IEventSubscription<TEvent> subscribtion)
        {
            _bus.Subscribe(subscribtion);
        }

        public void Unsubscribe<TEvent>(IEventSubscription<TEvent> subscribtion)
        {
            _bus.Unsubscribe(subscribtion);
        }
    }
}
