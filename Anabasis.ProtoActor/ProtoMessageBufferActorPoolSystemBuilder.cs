using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public class ProtoMessageBufferActorPoolSystemBuilder<TActor> : ProtoActorPoolSystem where TActor : MessageBufferActorBase
    {
        private readonly IBufferingStrategy[] _bufferingStrategies;

        public ProtoMessageBufferActorPoolSystemBuilder(IBufferingStrategy[] bufferingStrategies,
            ISupervisorStrategy supervisorStrategy,
            IServiceProvider serviceProvider,
            ISupervisorStrategy? chidSupervisorStrategy = null) : base(supervisorStrategy, serviceProvider, chidSupervisorStrategy)
        {
            _bufferingStrategies = bufferingStrategies;
        }
    }
}
