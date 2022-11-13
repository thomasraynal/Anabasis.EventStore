using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.TableStorage
{
    public class TableStorageClaim : IdentityRoleClaim<string>, ITableEntity
    {
        public string UserId { get; set; }
        public string Role { get; set; }
        [JsonIgnore]
        public string PartitionKey { get; set; }
        [JsonIgnore]
        public string RowKey { get; set; }
        [JsonIgnore]
        public DateTimeOffset? Timestamp { get; set; }
        [JsonIgnore]
        public ETag ETag { get; set; } = ETag.All;

    }
}