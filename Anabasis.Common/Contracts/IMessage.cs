using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IMessage
    {
        Guid EventID { get; }
        Guid CorrelationID { get; }
    }
}
