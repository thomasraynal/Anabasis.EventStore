using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.Actor;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure.Repository;

namespace Anabasis.Tests.Integration
{

  public class Trader : BaseAggregateActor<string, CurrencyPair>, IDisposable
  {

    private static readonly string[] CcyPairs = { "EUR/USD", "EUR/GBP" };

    private readonly CancellationTokenSource _cancel;
    private readonly Task _workProc;
    private readonly Random _rand = new Random();

    private readonly TraderConfiguration _configuration;

    public Trader(TraderConfiguration traderConfiguration,
      IEventStoreAggregateRepository<string> eventStoreRepository,
      IEventStoreCache<string, CurrencyPair> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
    {

      _cancel = new CancellationTokenSource();
      _configuration = traderConfiguration;

      _workProc = Task.Run(HandleWork);

    }

    public CurrencyPairPriceChanged Next()
    {
      var mid = _rand.NextDouble() * 10;
      var spread = _rand.NextDouble() * 2;

      var topic = CcyPairs[_rand.Next(0, CcyPairs.Count())];

      var price = new CurrencyPairPriceChanged(
          ask: mid + spread,
          bid: mid - spread,
          mid: mid,
          spread: spread,
          ccyPairId: topic,
          traderId: _configuration.Name
      );

      return price;
    }


    public async Task WaitUntilConnected()
    {
      while (!State.IsConnected)
      {
        await Task.Delay(2000);
      }
    }

    private async Task HandleWork()
    {

      while (!_cancel.IsCancellationRequested)
      {

        Thread.Sleep(_configuration.PriceGenerationDelay);

        if (_cancel.IsCancellationRequested) return;

        await WaitUntilConnected();

        if (State.IsConnected)
        {
          var changePrice = Next();

          await EmitEntityEvent(changePrice);

        }
      }
    }

    public override void Dispose()
    {
      _cancel.Cancel();
      _workProc.Dispose();

      base.Dispose();
    }
  }
}
