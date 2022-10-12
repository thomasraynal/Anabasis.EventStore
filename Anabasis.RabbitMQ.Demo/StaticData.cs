using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo
{
    public static class StaticData
    {

        public static UserCredentials UserCredentials => new("admin", "changeit");

        public const string MarketDataBusOne = "market-data-bus-one";
        public const string MarketDataBusTwo = "market-data-bus-two";

        public static string[] Desks { get; } = new[]
{
            "FX CASH",
            "FX Derivatives",
            "IR Derivatives",
            "Cross-Assets"
        };
        public static string[] Customers { get; } = new[]
        {
            "Bank of Andorra",
            "Bank of Europe",
            "Bank of England",
            "BCCI",
            "Abbey National",
            "Fx Shop",
            "Midland Bank",
            "National Bank of Alaska",
            "Northern Rock"
        };

        public static CurrencyPair[] CurrencyPairs { get; } = {
            new CurrencyPair("GBP/USD",1.6,4,5),
            new CurrencyPair("EUR/USD",1.23904,4,3),
            new CurrencyPair("EUR/GBP",0.7913,4,2),
            new CurrencyPair("NZD/CAD",0.8855,4,3)  ,
            new CurrencyPair("HKD/USD",0.128908,6,2) ,
            new CurrencyPair("NOK/SEK",1.10,3,2) ,
            new CurrencyPair("XAU/GBP",768.399,3,3) ,
            new CurrencyPair("USD/JPY",118.81,2,2),
        };

    }
}
