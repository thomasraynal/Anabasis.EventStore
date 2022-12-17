using Anabasis.Common.Worker;
using Anabasis.Common;
using Microsoft.Extensions.Logging;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public class ProtoMessageBufferActorPoolSystemBuilder<TActor> : ProtoActorSystem where TActor : MessageBufferActorBase
    {
        private readonly IBufferingStrategy[] _bufferingStrategies;

        public ProtoMessageBufferActorPoolSystemBuilder(IBufferingStrategy[] bufferingStrategies,
            ISupervisorStrategy supervisorStrategy,
            IProtoActorPoolDispatchQueueConfiguration protoActorPoolDispatchQueueConfiguration,
            IServiceProvider serviceProvider,
            ILoggerFactory? loggerFactory = null,
            IQueueBuffer? queueBuffer = null,
            ISupervisorStrategy? chidSupervisorStrategy = null,
            IKillSwitch? killSwitch = null) : base(supervisorStrategy,
                protoActorPoolDispatchQueueConfiguration,
                serviceProvider,
                loggerFactory,
                queueBuffer,
                chidSupervisorStrategy,
                killSwitch)
        {
            _bufferingStrategies = bufferingStrategies;
        }
    }
}
