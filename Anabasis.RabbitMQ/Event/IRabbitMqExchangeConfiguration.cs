using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ.Event
{
    public interface IRabbitMqExchangeConfiguration
    {
        string ExchangeName { get; }
        string ExchangeType { get; }
        bool CreateExchangeIfNotExist { get; }
        bool CreateDeadLetterExchangeIfNotExist { get; }
        bool IsAutoDelete { get; }
        bool IsDurable { get; }
    }
}
