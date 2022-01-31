using Anabasis.Api.Tests.Common;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EntityFramework.Tests.Integration
{
    [TestFixture]
    public class TestConnectionAndQuery
    {
        private Random _random;
        private TestDbContext _dbContext;

        [OneTimeSetUp]
        public void Setup()
        {

            if (TestHelper.IsAppVeyor)
            {
                Assert.Ignore("Cannot use SQLServer image in CI - too much RAM needed on th VM.");
            }
           
            _random = new Random();

            _dbContext = new TestDbContext();
            _dbContext.Database.Migrate();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            _dbContext.Dispose();
        }

        [Test,Order(1)]
        public async Task ShouldWireUpEFConnection()
        {
           
            var currencies = await _dbContext.Currencies.ToArrayAsync();

            Assert.IsNotEmpty(currencies);

            var currencyPairs = await _dbContext.CurrencyPairs.ToArrayAsync();

            Assert.IsNotEmpty(currencyPairs);

        }

        [Test, Order(2)]
        public async Task ShouldGenerateSomeTrades()
        {
            foreach(var trade in Generate(50))
            {
                _dbContext.Trades.Add(trade);
            }

            await _dbContext.SaveChangesAsync();
        }

        private Trade[] Generate(int numberToGenerate)
        {
            var counterparties = _dbContext.Counterparties.ToArray();
            var currencyPairs = _dbContext.CurrencyPairs.ToArray();

            var initialPrices = new Dictionary<string, decimal>()
            {
                {"EUR/USD",1.23904M },
                {"EUR/GBP",0.7913M },
                {"USD/JPY",118.81M}
            };


            Trade NewTrade(Counterparty[] counterparties, CurrencyPair[] currencyPairs, Dictionary<string, decimal> initialPrices)
            {

                var counterparty = counterparties[_random.Next(0, counterparties.Length)];

                var candidateCcyPairs = currencyPairs.Where(ccyPair => initialPrices.ContainsKey(ccyPair.Code)).ToArray();
                var currencyPair = candidateCcyPairs.ElementAt(_random.Next(0, candidateCcyPairs.Length));
                var amount = (_random.Next(1, 2000) / 2) * (10 ^ _random.Next(1, 5));
                var buySell = _random.Next(0, 2) == 1 ? BuyOrSell.Buy : BuyOrSell.Sell;

                var seconds = _random.Next(1, 60 * 60 * 24);
                var time = DateTime.Now.AddSeconds(-seconds);

                var price = GererateRandomPrice(currencyPair, buySell, initialPrices);

                initialPrices[currencyPair.Code] = price;

                return new Trade(Guid.NewGuid(),
                    currencyPair.Code,
                    counterparty.Name,
                    GererateRandomPrice(currencyPair, buySell, initialPrices),
                    amount,
                    buySell,
                    time);

            }

            return Enumerable.Range(1, numberToGenerate).Select(_ => NewTrade(counterparties, currencyPairs, initialPrices)).ToArray();

        }

        private decimal GererateRandomPrice(CurrencyPair currencyPair, BuyOrSell buyOrSell, Dictionary<string, decimal> currentPrices)
        {
            var price = currentPrices[currencyPair.Code];
            var pipsFromMarket = _random.Next(1, 100);
            var adjustment = Math.Round(pipsFromMarket * currencyPair.PipSize, currencyPair.DecimalPlaces);
            return buyOrSell == BuyOrSell.Sell ? price + adjustment : price - adjustment;
        }

    }
}
