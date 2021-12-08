using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface ISerializer
    {
        string ContentMIMEType { get; }
        string ContentEncoding { get; }

        byte[] SerializeObject(object obj);
        object DeserializeObject(byte[] str, Type type);
    }
}
