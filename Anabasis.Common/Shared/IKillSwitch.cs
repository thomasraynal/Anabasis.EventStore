﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IKillSwitch
    {
        void KillProcess(Exception exception);
        void KillProcess(string reason);
        void KillProcess(string reason, Exception exception);
    }
}
