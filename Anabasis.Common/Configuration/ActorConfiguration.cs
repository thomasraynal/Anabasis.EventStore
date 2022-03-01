using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public class ActorConfiguration : BaseConfiguration, IActorConfiguration
    {
        public static readonly ActorConfiguration Default = new();
        public ActorConfiguration(int actorMailBoxMessageBatchSize = 1, int actorMailBoxMessageQueueMaxSize = 10, bool crashAppOnError = true)
        {
            ActorMailBoxMessageBatchSize = actorMailBoxMessageBatchSize;
            ActorMailBoxMessageMessageQueueMaxSize = actorMailBoxMessageQueueMaxSize;
            CrashAppOnError = crashAppOnError;
        }

        public int ActorMailBoxMessageBatchSize { get; }
        public int ActorMailBoxMessageMessageQueueMaxSize { get; }
        public bool CrashAppOnError { get; }
    }
}
