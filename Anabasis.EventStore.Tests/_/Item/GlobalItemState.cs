using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Tests.Demo
{
    public class GlobalItemState
    {
        public static GlobalItemState Default = new GlobalItemState();

        public int Created { get; set; }
        public int Ready { get; set; }
        public int Deleted { get; set; }

        public override string ToString()
        {
            return $"{ItemState.Created} : {Created}" + Environment.NewLine +
                   $"{ItemState.Deleted} : {Deleted}" + Environment.NewLine +
                   $"{ItemState.Ready} : {Ready}" + Environment.NewLine;
        }
    }
}
