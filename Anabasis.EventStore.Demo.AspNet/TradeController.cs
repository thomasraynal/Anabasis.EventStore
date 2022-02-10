using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Anabasis.EventStore.Demo
{

    [ApiController]
    [Route("trades")]
    public class TradeController : ControllerBase
    {
        private readonly TradeSink _tradeSink;

        public TradeController(TradeSink tradeSink)
        {
            _tradeSink = tradeSink;
        }

        [HttpGet]
        public IActionResult GetTrades()
        {
            return Ok(_tradeSink.State.GetCurrents());
        }

        [HttpGet("{id}")]
        public IActionResult GetOneTrade(string tradeId)
        {
            var trade = _tradeSink.State.GetCurrents().FirstOrDefault(trade => trade.EntityId == tradeId);
            return Ok(trade);
        }

    }
}
