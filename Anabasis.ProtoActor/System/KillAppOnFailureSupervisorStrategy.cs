using Anabasis.Common;
using Proto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.System
{
    public class KillAppOnFailureSupervisorStrategy : ISupervisorStrategy
    {
        private readonly IKillSwitch _killSwitch;

        public KillAppOnFailureSupervisorStrategy(IKillSwitch killSwitch)
        {
            _killSwitch = killSwitch;
        }

        public void HandleFailure(ISupervisor supervisor, PID child, RestartStatistics rs, Exception cause, object? message)
        {
            _killSwitch.KillProcess($"supervisor => {supervisor.GetType()} child => {child} - message => {message}", cause);
        }
    }
}
