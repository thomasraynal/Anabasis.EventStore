using Newtonsoft.Json;
using System;

namespace Anabasis.Api
{
    public class UriJsonConverter : Newtonsoft.Json.JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Uri);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            if (reader.TokenType == JsonToken.String) return new Uri((string)reader.Value, UriKind.RelativeOrAbsolute);

            throw new InvalidOperationException("Unhandled case for UriConverter. Check to see if this converter has been applied to the wrong serialization type.");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (null == value)
            {
                writer.WriteNull();
                return;
            }

            var uri1 = value as Uri;
            if (uri1 != null)
            {

                if (uri1.IsAbsoluteUri)
                {
                    writer.WriteValue(uri1.AbsoluteUri);
                }
                else
                {
                    writer.WriteValue(uri1.ToString());
                }
                return;
            }

            throw new InvalidOperationException("Unhandled case for UriConverter. Check to see if this converter has been applied to the wrong serialization type.");
        }
    }
}
