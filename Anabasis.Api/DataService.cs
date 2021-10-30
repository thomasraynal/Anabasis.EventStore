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
    public class DataService : IDataService
    {
        public DataTable GetData()
        {
            var data = JsonConvert.DeserializeObject<JArray>(File.ReadAllText("./data/data.json"));

            return data.ToDataTable();
        }

    }
}
