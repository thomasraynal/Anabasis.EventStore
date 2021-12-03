using Anabasis.RabbitMQ;

namespace RabbitMQPlayground.Routing
{
    public interface IRabbitMqEventSerializer
    {
        ISerializer Serializer { get; }
        string GetRoutingKey(IRabbitMqEvent @event);
    }
}