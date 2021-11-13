using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Api.Converters
{
    public class RessourceJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Ressource);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var ressourceObjects = new List<RessourceObject>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.StartArray:
                        break;
                    case JsonToken.StartObject:
                        var ressourceObject = ReadObject(reader);
                        ressourceObjects.Add(ressourceObject);
                        break;
                    case JsonToken.EndArray:
                        return new Ressource(ressourceObjects.ToArray());
                }
            }

            throw new JsonSerializationException("Unexpected end when reading JArray");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var ressource = (Ressource)value;

            writer.WriteStartArray();

            foreach (var ressourceObject in ressource.RessourceObjects)
            {
                writer.WriteStartObject();

                foreach(var property in ressourceObject.Properties)
                {
                    writer.WritePropertyName(property.Key);
                    writer.WriteValue(property.Value);
                }
                
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        private RessourceObject ReadObject(JsonReader reader)
        {
            var ressourceProperties = new List<RessourceProperty>();

            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonToken.PropertyName:

                        var propertyName = reader.Value.ToString();

                        if (!reader.Read())
                            throw new JsonSerializationException("Unexpected end when reading JObject");

                        var propertyValue = ReadProperty(reader);

                        ressourceProperties.Add(new RessourceProperty(propertyName, propertyValue));
                        break;
                    case JsonToken.Comment:
                        break;
                    case JsonToken.EndObject:
                        return new RessourceObject(ressourceProperties.ToArray());
                }
            }

            throw new JsonSerializationException("Unexpected end when reading JObject");
        }

        private object ReadProperty(JsonReader reader)
        {
    
            switch (reader.TokenType)
            {
                case JsonToken.StartObject:
                    return ReadObject(reader);
                case JsonToken.StartArray:
                    return null;
                default:
                    return reader.Value;

                    throw new JsonSerializationException(String.Format("Unexpected token when converting ExpandoObject: {0}", reader.TokenType));
            }
        }
    }
}
