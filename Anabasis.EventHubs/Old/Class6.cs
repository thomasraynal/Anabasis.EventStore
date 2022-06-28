using Anabasis.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    [DataContract]
    [Serializable]
    public class AzureStorageConnectionOptions : BaseConfiguration
    {
        [JsonConstructor]
        public AzureStorageConnectionOptions()
        {
            AccountName = "(local)";
            AccountKey = null;
            UseHttps = true;
        }

        public AzureStorageConnectionOptions(string accountName, string accountKey)
        {
            AccountName = accountName;
            AccountKey = accountKey;
            UseHttps = true;
        }

        [DataMember]
        public string AccountName { get; set; }

        [DataMember]
        public string AccountKey { get; set; }


        private bool _useHttps;
        [DataMember]
        public bool UseHttps { get => _useHttps; set => _useHttps = true; } // force HTTPS


        public override string ToString() => GetStorageConnectionString();
        public string GetStorageConnectionString() => GetStorageConnectionString(AccountName, AccountKey, UseHttps);


        public static string GetStorageConnectionString(string accountName, string acountKey, bool _useHttps = true)
        {
            return $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={acountKey}";
        }

    }
}
