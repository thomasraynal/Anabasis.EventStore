﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.MessageBufferActor
{
    public class BufferTimeoutDelayMessage
    {
        public static readonly BufferTimeoutDelayMessage Instance = new();
        private BufferTimeoutDelayMessage()
        {
        }
    }
}
