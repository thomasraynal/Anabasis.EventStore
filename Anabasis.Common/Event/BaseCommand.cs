using Anabasis.Common;
using System;

namespace Anabasis.Common
{
    public abstract class BaseCommand : BaseEvent, ICommand
    {
        public BaseCommand(Guid correlationId, string streamId) : base(correlationId, streamId)
        {
            EntityId = streamId;
            IsCommand = true;
        }

    }
}
