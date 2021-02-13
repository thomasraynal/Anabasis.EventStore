using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public class DefaultSerializer : ISerializer
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };

        public object DeserializeObject(byte[] bytes, Type type)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type);
        }

        public byte[] SerializeObject(object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj, SerializerSettings));
        }
    }
}
