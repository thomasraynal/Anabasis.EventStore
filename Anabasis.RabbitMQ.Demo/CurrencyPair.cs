using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo
{
    public class CurrencyPair : BaseAggregate
    {
        public CurrencyPair(string code, double startingPrice, int decimalPlaces, double tickFrequency, int defaultSpread = 8)
        {
            EntityId = code;
            InitialPrice = startingPrice;
            DecimalPlaces = decimalPlaces;
            TickFrequency = tickFrequency;
            DefaultSpread = defaultSpread;
            PipSize = Math.Pow(10, -decimalPlaces);
        }

        public double InitialPrice { get; }
        public int DecimalPlaces { get; }
        public double TickFrequency { get; }
        public double PipSize { get; }
        public int DefaultSpread { get; }

        public override string ToString()
        {
            return $"Code: {EntityId}, DecimalPlaces: {DecimalPlaces}";
        }
    }
}
