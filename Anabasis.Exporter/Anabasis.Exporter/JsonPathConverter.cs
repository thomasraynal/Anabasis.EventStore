using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.Exporter
{

    public class JsonPathConverter : JsonConverter
    {
        private static readonly ConcurrentDictionary<Type, JsonConverter> _converterCache = new ConcurrentDictionary<Type, JsonConverter>();

        private void AddAndEnsurePropertySerializerIsPriorized(JsonSerializer jsonSerializer, JsonConverter propertyJsonConverter)
        {
            var converters = new List<JsonConverter>()
                        {
                            propertyJsonConverter
                        };

            var existingConverters = jsonSerializer.Converters.ToList();

            jsonSerializer.Converters.Clear();

            converters = converters.Concat(existingConverters).ToList();

            foreach (var converter in converters)
            {
                jsonSerializer.Converters.Add(converter);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jo = JObject.Load(reader);
            var targetObj = Activator.CreateInstance(objectType);

            foreach (var prop in objectType.GetProperties()
                                           .Where(p => p.CanRead && p.CanWrite))
            {
                var jsonPropertyAttribute = prop.GetCustomAttributes(true)
                              .OfType<JsonPropertyAttribute>()
                              .FirstOrDefault();

                var jsonConverterForProperty = prop.GetCustomAttributes(true)
                            .OfType<JsonConverterAttribute>()
                            .FirstOrDefault();

                var jsonPath = (jsonPropertyAttribute != null ? jsonPropertyAttribute.PropertyName : prop.Name);
                var token = jo.SelectToken(jsonPath);

                if (token != null && token.Type != JTokenType.Null)
                {
                    if (null != jsonConverterForProperty)
                    {
                        if (!_converterCache.ContainsKey(jsonConverterForProperty.ConverterType))
                        {
                            var converter = (JsonConverter)Activator.CreateInstance(jsonConverterForProperty.ConverterType);

                            _converterCache.AddOrUpdate(jsonConverterForProperty.ConverterType, converter, (type, _) =>
                            {
                                return converter;
                            });

                        }

                        AddAndEnsurePropertySerializerIsPriorized(serializer, _converterCache[jsonConverterForProperty.ConverterType]);

                    }

                    if (null != jsonPropertyAttribute.ItemConverterType)
                    {
                        if (!_converterCache.ContainsKey(jsonPropertyAttribute.ItemConverterType))
                        {
                            var converter = (JsonConverter)Activator.CreateInstance(jsonPropertyAttribute.ItemConverterType);

                            _converterCache.AddOrUpdate(jsonPropertyAttribute.ItemConverterType, converter, (type, _) =>
                            {
                                return converter;
                            });

                        }

                        AddAndEnsurePropertySerializerIsPriorized(serializer, _converterCache[jsonPropertyAttribute.ItemConverterType]);
                    }

                    var value = token.ToObject(prop.PropertyType, serializer);

                    prop.SetValue(targetObj, value, null);
                }
            }

            return targetObj;

        }

        public override bool CanConvert(Type objectType)
        {
            // CanConvert is not called when [JsonConverter] attribute is used
            return false;
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override void WriteJson(JsonWriter writer, object value,
                                       JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

}