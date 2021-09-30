using System;

namespace Anabasis.EventStore.Event
{
    public abstract class BaseCommand : BaseEvent, ICommand
    {
        public BaseCommand(Guid correlationId, string streamId) : base(correlationId, streamId)
        {
            StreamId = streamId;
            IsCommand = true;
        }

    }
}
