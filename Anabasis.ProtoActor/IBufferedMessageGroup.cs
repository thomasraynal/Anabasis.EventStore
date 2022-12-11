using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor
{
    public class BufferedMessageGroup: IBufferedMessageGroup
    {
        public BufferedMessageGroup() { }
        public BufferedMessageGroup(object[] bufferedMessages)
        {
            BufferedMessages = bufferedMessages;
        }

        public object[] BufferedMessages { get; internal set; }
    }

    public interface IBufferedMessageGroup
    {
        object[] BufferedMessages { get; }
    }
}
