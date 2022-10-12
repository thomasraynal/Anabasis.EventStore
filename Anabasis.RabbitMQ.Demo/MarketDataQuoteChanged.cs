using Anabasis.RabbitMQ.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo
{
    public class MarketDataQuoteChanged : BaseRabbitMqEvent
    {
        public MarketDataQuoteChanged(Guid eventID, Guid correlationId) : base(null, eventID, correlationId)
        {

        }

        [RoutingPosition(0)]
        public string CurrencyPair { get; set; }

        public double Bid { get; set; }
        public double Offer { get; set; }

    }
}
