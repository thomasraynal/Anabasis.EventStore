namespace Anabasis.Common
{
    public interface IActorConfiguration
    {
        int ActorMailBoxMessageMessageQueueMaxSize { get; }
        int ActorMailBoxMessageBatchSize { get; }
    }
}