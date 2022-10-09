namespace Anabasis.Common
{
    public interface IActorConfiguration
    {
        int ActorMailBoxMessageMessageQueueMaxSize { get; set; }
        int ActorMailBoxMessageBatchSize { get; set; }
        bool CrashAppOnError { get; set; }
    }
}