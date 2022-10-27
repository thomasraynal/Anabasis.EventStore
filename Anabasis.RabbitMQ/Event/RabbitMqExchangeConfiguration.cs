using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ.Event
{
    public class RabbitMqExchangeConfiguration : IRabbitMqExchangeConfiguration
    {
        public RabbitMqExchangeConfiguration(string exchangeName,
            string exchangeType, 
            bool createExchangeIfNotExist = true,
            bool isAutoDelete = false,
            bool isDurable = true)
        {
            ExchangeName = exchangeName;
            ExchangeType = exchangeType;
            CreateExchangeIfNotExist = createExchangeIfNotExist;
            IsAutoDelete = isAutoDelete;
            IsDurable = isDurable;
        }

        public string ExchangeName { get; }

        public string ExchangeType { get; }

        public bool CreateExchangeIfNotExist { get; }

        public bool IsAutoDelete { get; }

        public bool IsDurable { get; }
    }
}
