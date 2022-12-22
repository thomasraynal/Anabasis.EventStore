using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.AggregateActor
{
    public class EndCaughtUp
    {
        public static readonly EndCaughtUp Instance = new();
        private EndCaughtUp()
        {
        }
    }
}
