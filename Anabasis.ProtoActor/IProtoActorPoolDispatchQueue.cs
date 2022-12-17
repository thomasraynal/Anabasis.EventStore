using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{

    public interface IProtoActorPoolDispatchQueue : IDisposable
    {
        string Owner { get; }
        string Id { get; }
        bool IsFaulted { get; }
        Exception? LastError { get; }
        bool CanEnqueue();
        IMessage[] TryEnqueue(IMessage[] messages, out IMessage[] unProcessedMessages);
    }   
}
