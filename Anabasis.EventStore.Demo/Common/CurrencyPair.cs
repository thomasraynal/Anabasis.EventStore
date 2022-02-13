using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.EventStore.Demo
{
    //https://github.com/RolandPheasant/Dynamic.Trader
    public class CurrencyPair : BaseAggregate
    {
        public CurrencyPair(string code, decimal startingPrice, int decimalPlaces, decimal tickFrequency, int defaultSpread = 8)
        {
            EntityId = code;
            InitialPrice = startingPrice;
            DecimalPlaces = decimalPlaces;
            TickFrequency = tickFrequency;
            DefaultSpread = defaultSpread;
            PipSize = (decimal)Math.Pow(10, -decimalPlaces);
        }

        public decimal InitialPrice { get; }
        public int DecimalPlaces { get; }
        public decimal TickFrequency { get; }
        public decimal PipSize { get; }
        public int DefaultSpread { get; }

        public override string ToString()
        {
            return $"Code: {EntityId}, DecimalPlaces: {DecimalPlaces}";
        }
    }
}
