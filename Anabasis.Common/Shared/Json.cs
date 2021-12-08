using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public static class Json
    {
        private static JsonSerializerSettings _jsonSerializerSettings;

        public static void SetDefaultJsonSerializerSettings(JsonSerializerSettings jsonSerializerSettings)
        {
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        public static JsonSerializerSettings GetDefaultJsonSerializerSettings()
        {
            if (null != _jsonSerializerSettings) return _jsonSerializerSettings;

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };

            jsonSerializerSettings.Converters.Add(new UriJsonConverter());
            jsonSerializerSettings.Converters.Add(new StringEnumConverter());

            jsonSerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            jsonSerializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;

            return jsonSerializerSettings;

        }


    }
}
