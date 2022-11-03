using Azure;
using System;
using System.Linq;
using System.Runtime.Serialization;

namespace Anabasis.TableStorage
{
    [Serializable]
    internal class TableStorageBatchExecutionExceptions : Exception
    {
        public TableStorageBatchExecutionExceptions()
        {
        }

        public TableStorageBatchExecutionExceptions(string message) : base(message)
        {
        }

        public TableStorageBatchExecutionExceptions(Response[] batchErrors) : base("One or several tableTransactionAction are in error - see details for more informations")
        {
            Data["details"] = batchErrors.Select(batchError => new { status = batchError.Status, reasonPhrase = batchError.ReasonPhrase }).ToArray();
        }

        public TableStorageBatchExecutionExceptions(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TableStorageBatchExecutionExceptions(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}