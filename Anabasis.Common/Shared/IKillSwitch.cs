using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IKillSwitch
    {
        void KillMe(Exception exception);
        void KillMe(string reason);
    }
}
