using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.AggregateActor
{
    public class StartCaughtUp
    {
        public static readonly StartCaughtUp Instance = new();
        private StartCaughtUp()
        {
        }
    }
}
