using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EntityFramework.Tests.Integration
{
    public class CurrencyPair
    {
        private CurrencyPair()
        {
        }

        public CurrencyPair(string code,
            Currency currency1,
            Currency currency2,
            int decimalPlaces,
            decimal tickFrequency
            )
        {
            Code = code;
            Currency1 = currency1;
            Currency2 = currency2;
            DecimalPlaces = decimalPlaces;
            TickFrequency = tickFrequency;
        }

        public CurrencyPair(string code,
            string currency1Code,
            string currency2Code,
            int decimalPlaces,
            decimal tickFrequency
            )
        {
            Code = code;
            Currency1Code = currency1Code;
            Currency2Code = currency2Code;
            DecimalPlaces = decimalPlaces;
            TickFrequency = tickFrequency;
        }

        public string Code { get; set; }

        public string Currency1Code { get; set; }
        public string Currency2Code { get; set; }

        [ForeignKey("Currency1Code")]
        public Currency Currency1 { get; set; }

        [ForeignKey("Currency2Code")]
        public Currency Currency2 { get; set; }

        public int DecimalPlaces { get; set; }
        public decimal TickFrequency { get; set; }
        public decimal PipSize
        {
            get
            {
                return (decimal)Math.Pow(10, -DecimalPlaces);

            }
        }
    }
}
