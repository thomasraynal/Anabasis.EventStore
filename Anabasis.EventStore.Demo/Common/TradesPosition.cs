using System;

namespace Anabasis.EventStore.Demo
{
    public class TradesPosition
    {
        private readonly int _count;

        public TradesPosition(decimal buy, decimal sell, int count)
        {
            Buy = buy;
            Sell = sell;
            _count = count;
            Position = Buy - Sell;
        }

        public bool Negative => Position < 0;


        public decimal Position { get; }
        public decimal Buy { get; }
        public decimal Sell { get; }
    }
}
