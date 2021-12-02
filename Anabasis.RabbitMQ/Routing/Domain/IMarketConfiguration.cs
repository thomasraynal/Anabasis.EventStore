namespace RabbitMQPlayground.Routing
{
    public interface IMarketConfiguration
    {
        string EventExchange { get;  }
        string Name { get; }
    }
}