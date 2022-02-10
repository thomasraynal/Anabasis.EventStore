using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Anabasis.EventStore.Demo
{
    [ApiController]
    [Route("ccy")]
    public class PriceController : ControllerBase
    {
        private readonly MarketDataSink _marketDataSink;

        public PriceController(MarketDataSink marketDataSink)
        {
            _marketDataSink = marketDataSink;
        }

        [HttpGet]
        public IActionResult GetTradedCurrencyPairs()
        {
            return Ok(_marketDataSink.State.GetCurrents());
        }

        [HttpGet("{id}")]
        public IActionResult GetOneTradedCurrencyPair(string currencyId)
        {
            var currencyPair = _marketDataSink.State.GetCurrents().FirstOrDefault(marketData => marketData.EntityId == currencyId);
            return Ok(currencyPair);
        }

    }
}
