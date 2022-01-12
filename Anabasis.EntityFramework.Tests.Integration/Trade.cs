using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EntityFramework.Tests.Integration
{
    public class Trade
    {
        public Trade(Guid tradeId, 
            string currencyPairCode, 
            string counterpartyCode,
            decimal price, 
            long amount,
            BuyOrSell buyOrSell,
            DateTime timestampUtc)
        {
            TradeId = tradeId;
            CurrencyPairCode = currencyPairCode;
            CounterpartyCode = counterpartyCode;
            Price = price;
            Amount = amount;
            BuyOrSell = buyOrSell;
            TimestampUtc = timestampUtc;
        }

        public Trade(Guid tradeId,
            CurrencyPair currencyPair,
            Counterparty counterparty,
            decimal price,
            long amount,
            BuyOrSell buyOrSell,
            DateTime timestampUtc)
        {
            TradeId = tradeId;
            CurrencyPair  = currencyPair;
            Counterparty = counterparty;
            CurrencyPairCode = currencyPair.Code;
            CounterpartyCode = counterparty.Name;
            Price = price;
            Amount = amount;
            BuyOrSell = buyOrSell;
            TimestampUtc = timestampUtc;
        }

        private Trade()
        {
        }

        public Guid TradeId { get; set; }

        public string CurrencyPairCode { get; set; }
        [ForeignKey("CurrencyPairCode")]
        public CurrencyPair CurrencyPair { get; set; }

        public string CounterpartyCode { get; set; }
        [ForeignKey("CounterpartyCode")]
        public Counterparty Counterparty { get; set; }

        public decimal Price { get; set; }

        public long Amount { get; set; }

        public BuyOrSell BuyOrSell { get; set; }

        public DateTime TimestampUtc { get; set; }
    }
}
