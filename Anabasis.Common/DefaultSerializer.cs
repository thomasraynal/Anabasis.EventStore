using System;

namespace Anabasis.Common
{
    public class DefaultSerializer : ISerializer
    {
        public string ContentMIMEType => "application/json";
        public string ContentEncoding => string.Empty;

        public object DeserializeObject(byte[] bytes, Type type)
        {
            return bytes.JsonTo(type);
        }

        public byte[] SerializeObject(object obj)
        {
            return obj.ToJsonBytes();
        }
    }
}
