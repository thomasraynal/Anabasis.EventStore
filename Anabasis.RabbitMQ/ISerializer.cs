using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ
{
    public interface ISerializer
    {
        byte[] SerializeObject(object obj);
        object DeserializeObject(byte[] str, Type type);
    }
}
