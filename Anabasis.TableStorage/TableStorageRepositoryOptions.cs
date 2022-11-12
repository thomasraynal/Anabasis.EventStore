using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.TableStorage
{
    public class TableStorageRepositoryOptions
    {
        public Uri StorageUri { get; set; }
        public TableSharedKeyCredential TableSharedKeyCredential { get; set; }
    }
}
