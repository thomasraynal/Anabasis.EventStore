using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.TableStorage
{
    public class TableStorageUserLoginInfo : UserLoginInfo, ITableEntity
    {
        public TableStorageUserLoginInfo() : base("default", "default", "default")
        {

        }

        public TableStorageUserLoginInfo(string loginProvider, string providerKey, string displayName) : base(loginProvider, providerKey, displayName)
        {
        }

        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;
    }
}
