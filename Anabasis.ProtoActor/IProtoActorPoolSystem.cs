using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public interface IProtoActorPoolSystem: IProtoActorSystem
    {
        Task Send(IMessage[] messages, TimeSpan? timeout = null);
    }
}
