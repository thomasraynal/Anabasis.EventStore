using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public class DefaultSerializer : ISerializer
  {
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
