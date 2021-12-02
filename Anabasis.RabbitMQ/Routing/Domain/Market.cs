using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQPlayground.Routing.Event;

namespace RabbitMQPlayground.Routing.Domain
{
    public class Market : IPublisher, ICommandHandler, IDisposable
    {
        private readonly IBus _bus;

        public List<CurrencyPair> CurrencyPairs { get; }

        public IMarketConfiguration Configuration { get; private set; }

        public Market(IMarketConfiguration marketConfiguration, IBusConfiguration configuration, IConnection connection)
        {
            var container = BusFactory.CreateContainer<RegistryForTests>(connection, configuration);

            _bus = container.GetInstance<IBus>();

            Configuration = marketConfiguration;

            CurrencyPairs = new List<CurrencyPair>();

            _bus.Handle(new CommandSubscription<ChangePriceCommand, ChangePriceCommandResult>(Configuration.Name, (command) =>
            {
                var ccyPair = CurrencyPairs.FirstOrDefault(ccy => ccy.Id == command.AggregateId);

                if (null == ccyPair)
                {
                    ccyPair = new CurrencyPair(command.AggregateId);
                    CurrencyPairs.Add(ccyPair);
                }

                ccyPair.Ask = command.Ask;
                ccyPair.Bid = command.Bid;

                ccyPair.AppliedEvents.Add(command);

                Emit(new PriceChangedEvent(command.AggregateId)
                {
                    Ask = command.Ask,
                    Bid = command.Bid,
                    Counterparty = command.Counterparty,
                }, Configuration.EventExchange);

                return new ChangePriceCommandResult()
                {
                    Market = Configuration.Name
                };

         
            }));
        }

        public void Emit(IEvent @event, string exchange)
        {
            _bus.Emit(@event, exchange);
        }

        public async Task<TCommandResult> Send<TCommandResult>(ICommand command) where TCommandResult : ICommandResult
        {
            return await _bus.Send<TCommandResult>(command);
        }

        public void Dispose()
        {
            _bus.Dispose();
        }

        public void Handle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
                   where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            _bus.Handle(subscription);
        }

        public void UnHandle<TCommand, TCommandResult>(ICommandSubscription<TCommand, TCommandResult> subscription)
               where TCommand : class, ICommand
             where TCommandResult : ICommandResult
        {
            _bus.UnHandle(subscription);
        }
    }
}
