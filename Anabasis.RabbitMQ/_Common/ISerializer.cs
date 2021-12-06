using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface ISerializer
    {
        string ContentMIMEType { get; }
        string ContentEncoding { get; }

        T Deserialize<T>(byte[] bytes);
        object Deserialize(byte[] bytes, Type type);
        byte[] Serialize(object obj);
    }
}
