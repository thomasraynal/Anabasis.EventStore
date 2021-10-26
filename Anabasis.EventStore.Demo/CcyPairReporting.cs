using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class CcyPairReporting : Dictionary<string, (decimal bid, decimal offer, decimal spread, bool IsUp)>
    {
        public static CcyPairReporting Empty = new();

        public CcyPairReporting(CcyPairReporting previous)
        {
            foreach (var keyValue in previous)
            {
                this[keyValue.Key] = keyValue.Value;
            }
        }

        public CcyPairReporting()
        {
        }
    }
}
