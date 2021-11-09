using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class Ressource
    {
        [JsonConstructor]
        internal Ressource()
        {
        }

        public Ressource(RessourceObject[] ressourceObjects)
        {
            RessourceObjects = ressourceObjects;
        }

        [Required]
        [JsonProperty(Required = Required.DisallowNull)]
        public RessourceObject[] RessourceObjects { get; set; }
    }

    public class RessourceObject
    {
        [JsonConstructor]
        internal RessourceObject()
        {
        }

        public RessourceObject(RessourceProperty[] properties)
        {
            Properties = properties;
        }

        [Required]
        public string Id => Properties.FirstOrDefault(property => property.Key == "uniqueId")?.Value as string;

        [Required]
        public string Name => Properties.FirstOrDefault(property => property.Key == "name")?.Value as string;

        [Required]
        [JsonProperty(Required = Required.DisallowNull)]
        public RessourceProperty[] Properties { get; set; }
    }

    public class RessourceProperty
    {
        [JsonConstructor]
        internal RessourceProperty()
        {
        }

        public RessourceProperty(string key, object value)
        {
            Key = key;
            Value = value;
        }

        [Required]
        [JsonProperty(Required = Required.DisallowNull)]
        public string Key { get; set; }

        [Required]
        public object Value { get; set; }
    }
}
