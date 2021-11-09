using Newtonsoft.Json.Linq;
using System.Data;

namespace Anabasis.Api
{
    public interface IDataService
    {
        Ressource GetData();
    }
}