using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Text;

namespace Anabasis.EventStore
{
  public static class JsonExtensions
  {
    public static byte[] ToJsonBytes(this object obj, Encoding encoding = null)
    {
      var jsonSerializerSettings = GetDefaultJsonSerializerSettings();
      var jsonEncoding = encoding ?? Encoding.UTF8;

      return jsonEncoding.GetBytes(JsonConvert.SerializeObject(obj, jsonSerializerSettings));
    }

    public static string ToJson(this object obj, Encoding encoding = null)
    {
      var jsonSerializerSettings = GetDefaultJsonSerializerSettings();
      return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
    }

    public static T JsonTo<T>(this byte[] bytes, Encoding encoding = null)
    {

      var jsonSerializerSettings = GetDefaultJsonSerializerSettings();
      var jsonEncoding = encoding ?? Encoding.UTF8;

      return JsonConvert.DeserializeObject<T>(jsonEncoding.GetString(bytes), jsonSerializerSettings);

    }

    public static T JsonTo<T>(this string str)
    {
      var jsonSerializerSettings = GetDefaultJsonSerializerSettings();
      return JsonConvert.DeserializeObject<T>(str, jsonSerializerSettings);
    }

    public static object JsonTo(this byte[] bytes, Type type, Encoding encoding = null)
    {
      var jsonSerializerSettings = GetDefaultJsonSerializerSettings();
      var jsonEncoding = encoding ?? Encoding.UTF8;

      return JsonConvert.DeserializeObject(jsonEncoding.GetString(bytes), type, jsonSerializerSettings);

    }

    public static JsonSerializerSettings GetDefaultJsonSerializerSettings()
    {

      return new JsonSerializerSettings()
      {
        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy()
        },
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
      };

    }
  }

}
