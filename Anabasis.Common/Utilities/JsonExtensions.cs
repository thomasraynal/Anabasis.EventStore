using Newtonsoft.Json;
using System;
using System.Text;

namespace Anabasis.Common
{
    public static class JsonExtensions
    {
        public static byte[] ToJsonBytes(this object obj, Encoding? encoding = null)
        {
            var jsonSerializerSettings = Json.GetDefaultJsonSerializerSettings();
            var jsonEncoding = encoding ?? Encoding.UTF8;

            return jsonEncoding.GetBytes(JsonConvert.SerializeObject(obj, jsonSerializerSettings));
        }

        public static string ToJson(this object obj)
        {
            var jsonSerializerSettings = Json.GetDefaultJsonSerializerSettings();
            return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
        }

        public static T JsonTo<T>(this byte[] bytes, Encoding? encoding = null)
        {

            var jsonSerializerSettings = Json.GetDefaultJsonSerializerSettings();
            var jsonEncoding = encoding ?? Encoding.UTF8;

            return JsonConvert.DeserializeObject<T>(jsonEncoding.GetString(bytes), jsonSerializerSettings);
        }

        public static T JsonTo<T>(this string str)
        {
            var jsonSerializerSettings = Json.GetDefaultJsonSerializerSettings();
            return JsonConvert.DeserializeObject<T>(str, jsonSerializerSettings);
        }

        public static object JsonTo(this byte[] bytes, Type type, Encoding? encoding = null)
        {
            var jsonSerializerSettings = Json.GetDefaultJsonSerializerSettings();
            var jsonEncoding = encoding ?? Encoding.UTF8;

            return JsonConvert.DeserializeObject(jsonEncoding.GetString(bytes), type, jsonSerializerSettings);
        }

    }

}
