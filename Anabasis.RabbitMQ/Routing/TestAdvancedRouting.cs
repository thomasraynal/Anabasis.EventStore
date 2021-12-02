using NUnit.Framework;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQPlayground.Routing.Domain;
using RabbitMQPlayground.Routing.Event;
using RabbitMQPlayground.Routing.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{

    [TestFixture]
    public class TestAdvancedRouting
    {
        private readonly string _fxEventsExchange = "fx";
        private readonly string _fxRejectedEventsExchange = "fx-rejected";
        private readonly string _marketExchange = "fxconnect";

        class TestEvent : EventBase
        {
            public TestEvent(string aggregateId) : base(aggregateId)
            {
            }

            public object Invalid { get; set; }

            public string Broker { get; set; }

            [RoutingPosition(1)]
            public string Market { get; set; }

            [RoutingPosition(2)]
            public string Counterparty { get; set; }

            [RoutingPosition(3)]
            public string Exchange { get; set; }


        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.ExchangeDelete(_fxEventsExchange);
                channel.ExchangeDelete(_fxRejectedEventsExchange);
                channel.ExchangeDelete(Bus.CommandsExchange);
                channel.ExchangeDelete(Bus.RejectedCommandsExchange);
            }
        }


        [Test]
        public void ShouldNotSerializeAnEventAsRabbitSubject()
        {

            //non routable property
            Expression<Func<TestEvent, bool>> validExpression = (ev) => (ev.AggregateId == "MySmallBusiness" && (ev.Broker == "Newedge" && ev.Counterparty == "SGCIB") && ev.Exchange == "SmallCap");

            var visitor = new RabbitMQSubjectExpressionVisitor(typeof(TestEvent));

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });


            //type not allowed
            validExpression = (ev) => ev.Invalid ==  null;

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });


            //member multiple reference
            validExpression = (ev) => ev.AggregateId == "EUR/USD"  && ev.AggregateId == "EUR/GBP";

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });

            //too complex lambda 
            validExpression = (ev) => ev.AggregateId == "EUR/USD" && ev.Market != "Euronext";

            Assert.Throws<InvalidOperationException>(() =>
            {
                visitor.Visit(validExpression);
            });
 
        }

        [Test]
        public void ShouldSerializeAnEventAsRabbitSubject()
        {

            Expression<Func<TestEvent, bool>> validExpression = (ev) => (ev.AggregateId == "MySmallBusiness" &&  (ev.Market == "Euronext" && ev.Counterparty == "SGCIB") && ev.Exchange == "SmallCap");

            var visitor = new RabbitMQSubjectExpressionVisitor(typeof(TestEvent));

            visitor.Visit(validExpression);

            var subject = visitor.Resolve();

            Assert.AreEqual("MySmallBusiness.Euronext.SGCIB.SmallCap", subject);

            validExpression = (ev) => true;

            visitor.Visit(validExpression);

            subject = visitor.Resolve();

            Assert.AreEqual("#", subject);

            validExpression = (ev) => ev.AggregateId == "MySmallBusiness";

            visitor.Visit(validExpression);

            subject = visitor.Resolve();

            Assert.AreEqual("MySmallBusiness.*", subject);

            validExpression = (ev) => ev.Market == "Euronext";

            visitor.Visit(validExpression);

            subject = visitor.Resolve();

            Assert.AreEqual("*.Euronext.*", subject);

        }

        [Test]
        public async Task ShouldSendMultipleCommands()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration();

            var marketConfiguration = new MarketConfiguration(_marketExchange, _fxEventsExchange);
            var traderConfiguration = new TraderConfiguration(_fxEventsExchange, (ev) => true);

            var traderConnection = factory.CreateConnection();
            var marketConnection = factory.CreateConnection();

            using (var trader = new Trader(traderConfiguration, busConfiguration, traderConnection))
            using (var market = new Market(marketConfiguration, busConfiguration, marketConnection))
            {

                trader.Subscribe(new EventSubscription<CurrencyPairDesactivated>(_fxEventsExchange, (ev) => true, (ev) =>
                {
                    var ccyP = trader.CurrencyPairs.First(ccy => ccy.Id == ev.AggregateId);
                    ccyP.AppliedEvents.Add(ev);
                }));

                market.Handle(new CommandSubscription<DesactivateCurrencyPairCommand, DesactivateCurrencyPairCommandResult>(_marketExchange, (cmd) =>
                 {
                     market.Emit(new CurrencyPairDesactivated(cmd.AggregateId), _fxEventsExchange);

                     return new DesactivateCurrencyPairCommandResult()
                     {
                         Market = market.Configuration.Name
                     };

                 }));

                var changePriceCommand = new ChangePriceCommand("EUR/USD", market.Configuration.Name)
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                var desactivateCcyPairCommand = new DesactivateCurrencyPairCommand("EUR/USD", market.Configuration.Name)
                {
                };


                var changePriceCommmandResult = await trader.Send<ChangePriceCommandResult>(changePriceCommand);

                Assert.IsNotNull(changePriceCommmandResult);
                Assert.AreEqual(market.Configuration.Name, changePriceCommmandResult.Market);

                await Task.Delay(200);

                Assert.AreEqual(1, trader.CurrencyPairs.Count);

                var ccyPair = trader.CurrencyPairs.First();

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var desactivateCccyPairCommmandResult = await trader.Send<DesactivateCurrencyPairCommandResult>(desactivateCcyPairCommand);

                Assert.IsNotNull(desactivateCccyPairCommmandResult);
                Assert.AreEqual(market.Configuration.Name, desactivateCccyPairCommmandResult.Market);

                await Task.Delay(200);

                Assert.AreEqual(2, ccyPair.AppliedEvents.Count);

            }
        }

        [Test]
        public async Task ShouldSendCommand()
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration();

            var traderConnection = factory.CreateConnection();
            var marketConnection = factory.CreateConnection();

            var marketConfiguration = new MarketConfiguration(_marketExchange, _fxEventsExchange);
            var traderConfiguration = new TraderConfiguration(_fxEventsExchange, (ev) => true);

            using (var trader = new Trader(traderConfiguration, busConfiguration, traderConnection))
            using (var market = new Market(marketConfiguration, busConfiguration, marketConnection))
            {

                var command = new ChangePriceCommand("EUR/USD", market.Configuration.Name)
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                var commmandResult = await trader.Send<ChangePriceCommandResult>(command);

                Assert.IsNotNull(commmandResult);
                Assert.AreEqual(market.Configuration.Name, commmandResult.Market);

                await Task.Delay(200);

                Assert.AreEqual(1, trader.CurrencyPairs.Count);

                var ccyPair = trader.CurrencyPairs.First();

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var appliedEvent = ccyPair.AppliedEvents.First() as PriceChangedEvent;

                Assert.AreEqual(command.AggregateId, ccyPair.Id);
                Assert.AreEqual(command.Ask, ccyPair.Ask);
                Assert.AreEqual(command.Bid, ccyPair.Bid);
                Assert.AreEqual(command.Counterparty, appliedEvent.Counterparty);

            }

        }

        [Test]
        public async Task ShouldFilterByTopic()
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };
            var busConfiguration = new BusConfiguration();
            var traderConnection = factory.CreateConnection();
            var traderConfiguration = new TraderConfiguration(_fxEventsExchange, (ev) => ev.Counterparty == "SGCIB");

            using (var trader = new Trader(traderConfiguration, busConfiguration, traderConnection))
            {
           
                var validEvent1 = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                trader.Emit(validEvent1);

                await Task.Delay(50);

                Assert.AreEqual(1, trader.CurrencyPairs.Count);

                var ccyPair = trader.CurrencyPairs.First();

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var invalidEvent = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "BNP"
                };

                trader.Emit(invalidEvent);

                await Task.Delay(50);

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var validEvent2 = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                trader.Emit(validEvent2);

                await Task.Delay(50);

                Assert.AreEqual(2, ccyPair.AppliedEvents.Count);
            }

        }

        [Test]
        public async Task ShouldSubscribeAndUnsubscribe()
        {

            var serializer = new JsonNetSerializer();
            var eventSerializer = new EventSerializer(serializer);
            var logger = new LoggerForTests();
            var factory = new ConnectionFactory() { HostName = "localhost" };
            var busConfiguration = new BusConfiguration();

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {

                channel.ExchangeDeclare(exchange: _fxEventsExchange, type: "topic", durable: false, autoDelete: true);


                var bus = new Bus(busConfiguration, connection, logger, eventSerializer);

                var sgcibHasBeenCalled = false;
                var bnpHasBeenCalled = false;

                void reset()
                {
                    sgcibHasBeenCalled = false;
                    bnpHasBeenCalled = false;
                };

                var sgcibSubscription = new EventSubscription<PriceChangedEvent>(_fxEventsExchange, ev => ev.Counterparty == "SGCIB", (@event) =>
                {
                    sgcibHasBeenCalled = true;
                });

                var bnpSubscription = new EventSubscription<PriceChangedEvent>(_fxEventsExchange, ev => ev.Counterparty == "BNP", (@event) =>
                {
                    bnpHasBeenCalled = true;
                });

                var sgcibEvent = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                var bnpEvent = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "BNP"
                };

                bus.Emit(sgcibEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsFalse(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

                bus.Subscribe(sgcibSubscription);

                bus.Emit(sgcibEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsTrue(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

                reset();

                bus.Subscribe(bnpSubscription);

                bus.Emit(bnpEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsFalse(sgcibHasBeenCalled);
                Assert.IsTrue(bnpHasBeenCalled);

                reset();

                bus.Unsubscribe(bnpSubscription);

                bus.Emit(bnpEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsFalse(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

                bus.Emit(sgcibEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsTrue(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

                reset();

                bus.Unsubscribe(sgcibSubscription);

                bus.Emit(sgcibEvent, _fxEventsExchange);
                await Task.Delay(50);

                Assert.IsFalse(sgcibHasBeenCalled);
                Assert.IsFalse(bnpHasBeenCalled);

            }

        }

        [Test]
        public async Task ShouldFailedToConsumeEvent()
        {
   
            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration();
            var traderConfiguration = new TraderConfiguration(_fxEventsExchange, (ev) => true);
            var traderConnection = factory.CreateConnection();

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            using (var trader = new Trader(traderConfiguration, busConfiguration, traderConnection))
            {
      
                var deadLettersQueue = channel.QueueDeclare(exclusive: true, autoDelete: true).QueueName;
                var consumer = new EventingBasicConsumer(channel);

                channel.QueueBind(queue: deadLettersQueue,
                     exchange: _fxRejectedEventsExchange,
                     routingKey: "#");

                channel.BasicConsume(queue: deadLettersQueue,
                                     autoAck: false,
                                     consumer: consumer);

                var hasReceivedDeadLetter = false;

                consumer.Received += (model, arg) =>
                {
                    hasReceivedDeadLetter = true;
                };

              var body = Encoding.UTF8.GetBytes("this will explode server side");

                channel.BasicPublish(exchange: _fxEventsExchange,
                                     routingKey: "#",
                                     basicProperties: null,
                                     body: body);

                await Task.Delay(500);

                Assert.IsTrue(hasReceivedDeadLetter);

            }

        }

        [Test]
        public void ShouldFailedToHandleCommand()
        {

            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration()
            {
                CommandTimeout = TimeSpan.FromMilliseconds(500)
            };

            var traderConnection = factory.CreateConnection();
            var traderConfiguration = new TraderConfiguration(_fxEventsExchange, (ev) => true);

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            using (var trader = new Trader(traderConfiguration, busConfiguration, traderConnection))
      
            {
          
                var deadLettersQueue = channel.QueueDeclare(exclusive: true, autoDelete: true).QueueName;
                var consumer = new EventingBasicConsumer(channel);

                channel.QueueBind(queue: deadLettersQueue,
                     exchange: Bus.RejectedCommandsExchange,
                     routingKey: "#");

                channel.BasicConsume(queue: deadLettersQueue,
                                     autoAck: false,
                                     consumer: consumer);

                var hasReceivedDeadLetter = false;

                consumer.Received += (model, arg) =>
                {
                    hasReceivedDeadLetter = true;
                };

                Assert.ThrowsAsync<TaskCanceledException>(async () =>
                {
                    await trader.Send<ChangePriceCommandResult>(new ChangePriceCommand("EUR/USD", _marketExchange));
                });

                var marketConnection = factory.CreateConnection();
                var marketConfiguration = new MarketConfiguration(_marketExchange, _fxEventsExchange);

                //create an handler for the command
                using (var market = new Market(marketConfiguration, busConfiguration, marketConnection))
                {
                    market.Handle(new CommandSubscription<FaultyCommand, CommandResult>(market.Configuration.Name, (cmd) => new CommandResult()));

                    Assert.ThrowsAsync<CommandFailureException>(async () =>
                    {
                        await trader.Send<CommandResult>(new FaultyCommand("EUR/USD", market.Configuration.Name));
                    });

                    Assert.IsTrue(hasReceivedDeadLetter);

                }

            }
        }

        [Test]
        public async Task ShouldConsumeMultipleEvents()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration();

            var traderConnection = factory.CreateConnection();
            var traderConfiguration = new TraderConfiguration(_fxEventsExchange, (ev) => true);

            using (var trader = new Trader(traderConfiguration, busConfiguration, traderConnection))
            {

                trader.Subscribe(new EventSubscription<CurrencyPairDesactivated>(_fxEventsExchange, (ev) => true, (ev) =>
                {
                    var ccyP = trader.CurrencyPairs.First(ccy => ccy.Id == ev.AggregateId);
                    ccyP.AppliedEvents.Add(ev);
                }));

                trader.Emit(new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                });

                await Task.Delay(100);

                Assert.AreEqual(1, trader.CurrencyPairs.Count);

                var ccyPair = trader.CurrencyPairs.First();

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                trader.Emit(new CurrencyPairDesactivated("EUR/USD"));

                await Task.Delay(100);

                Assert.AreEqual(2, ccyPair.AppliedEvents.Count);

            }
        }
    

        [Test]
        public async Task ShouldConsumeEvent()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };

            var busConfiguration = new BusConfiguration();

            var traderConnection = factory.CreateConnection();
            var traderConfiguration = new TraderConfiguration(_fxEventsExchange, (ev) => true);

            using (var trader = new Trader(traderConfiguration, busConfiguration, traderConnection))
            {

                var emittedEvent = new PriceChangedEvent("EUR/USD")
                {
                    Ask = 1.25,
                    Bid = 1.15,
                    Counterparty = "SGCIB"
                };

                trader.Emit(emittedEvent);

                await Task.Delay(50);

                Assert.AreEqual(1, trader.CurrencyPairs.Count);

                var ccyPair = trader.CurrencyPairs.First();

                Assert.AreEqual(1, ccyPair.AppliedEvents.Count);

                var appliedEvent = ccyPair.AppliedEvents.First() as PriceChangedEvent;

                Assert.AreEqual(emittedEvent.AggregateId, ccyPair.Id);
                Assert.AreEqual(emittedEvent.Ask, ccyPair.Ask);
                Assert.AreEqual(emittedEvent.Bid, ccyPair.Bid);
                Assert.AreEqual(emittedEvent.Counterparty, appliedEvent.Counterparty);

         
            }


        }

    }
}
