using Anabasis.Common;
using System;

namespace Anabasis.Common
{
    public abstract class BaseCommand : BaseEvent, ICommand
    {
        public BaseCommand(string entityId, Guid? correlationId = null, Guid? causeId = null) : base(entityId, correlationId, causeId)
        {
            EntityId = entityId;
            IsCommand = true;
        }

    }
}
