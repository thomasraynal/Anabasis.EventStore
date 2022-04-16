using System;
using System.Reactive.Concurrency;
using System.Runtime.ExceptionServices;

namespace Anabasis.Common
{
    public class KillSwitch: IKillSwitch
    {
        public void KillProcess(Exception exception)
        {
            Scheduler.Default.Schedule(() => ExceptionDispatchInfo.Capture(exception).Throw());
        }

        public void KillMe(string reason)
        {
            KillProcess(new Exception(reason));
        }
    }
}
