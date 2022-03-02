using System;
using System.Reactive.Concurrency;
using System.Runtime.ExceptionServices;

namespace Anabasis.Common
{
    public class KillSwitch: IKillSwitch
    {
        public void KillMe(Exception exception)
        {
            Scheduler.Default.Schedule(() => ExceptionDispatchInfo.Capture(exception).Throw());
        }

        public void KillMe(string reason)
        {
            KillMe(new Exception(reason));
        }
    }
}
