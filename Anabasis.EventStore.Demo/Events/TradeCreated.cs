using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class TradeCreated : BaseAggregateEvent<Trade>
    {
        public TradeCreated(string entityId, Guid correlationId) : base($"{entityId}", correlationId)
        {
        }

        public string CurrencyPair { get; set; }
        public string Customer { get; set; }
        public decimal TradePrice { get; set; }
        public decimal MarketPrice { get; set; }
        public decimal PercentFromMarket { get; set; }
        public decimal Amount { get; set; }
        public BuyOrSell BuyOrSell { get; set; }
        public TradeStatus Status { get; set; }
        public DateTime Timestamp { get; set; }

        protected override void ApplyInternal(Trade entity)
        {
            entity.CurrencyPair = CurrencyPair;
            entity.Customer = Customer;
            entity.TradePrice = TradePrice;
            entity.MarketPrice = MarketPrice;
            entity.PercentFromMarket = PercentFromMarket;
            entity.Amount = Amount;
            entity.BuyOrSell = BuyOrSell;
            entity.Status = Status;
            entity.Timestamp = Timestamp;
        }
    }
}
