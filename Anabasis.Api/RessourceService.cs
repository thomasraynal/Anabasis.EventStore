using Anabasis.Api.Converters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class RessourceService : IDataService
    {
        public Ressource GetData()
        {
            var settings = Json.GetDefaultJsonSerializerSettings();
            settings.Converters.Add(new RessourceJsonConverter());

            return JsonConvert.DeserializeObject<Ressource>(File.ReadAllText("./data/data.json"), settings);

        }

    }
}
