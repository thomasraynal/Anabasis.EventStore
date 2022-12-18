using Anabasis.Common.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Anabasis.Common;

namespace Anabasis.ProtoActor
{
    public abstract class AcknowledgableMessageBufferActorBase<TAcknowledgable> : MessageBufferActorBase<TAcknowledgable>
        where TAcknowledgable : class, IAcknowledgable
    {
        protected AcknowledgableMessageBufferActorBase(MessageBufferActorConfiguration messageBufferActorConfiguration, ILoggerFactory? loggerFactory = null) : base(messageBufferActorConfiguration, loggerFactory)
        {
        }

        protected override async Task OnBufferConsumed(TAcknowledgable[] buffer)
        {
            await buffer.Select(message => message.Acknowledge()).ExecuteAndWaitForCompletion();
        }
    }
}
