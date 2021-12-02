namespace RabbitMQPlayground.Routing
{
    public interface IEventSerializer
    {
        ISerializer Serializer { get; }
        string GetRoutingKey(IEvent @event);
    }
}