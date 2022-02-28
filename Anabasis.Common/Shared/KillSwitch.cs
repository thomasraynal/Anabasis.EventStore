using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Anabasis.Common
{
    public static class KillSwitch
    {
        public static void KillMe(Exception exception)
        {
            Scheduler.Default.Schedule(() => ExceptionDispatchInfo.Capture(exception).Throw());
        }
    } 
}
