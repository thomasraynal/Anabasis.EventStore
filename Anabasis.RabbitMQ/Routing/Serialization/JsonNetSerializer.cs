using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class JsonNetSerializer : ISerializer
    {
        public string ContentMIMEType => "application/json";

        public string ContentEncoding => string.Empty;

        public T Deserialize<T>(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }

        public object Deserialize(byte[] bytes, Type type)
        {
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(bytes), type);
        }

        public byte[] Serialize(object obj)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
        }
    }
}
