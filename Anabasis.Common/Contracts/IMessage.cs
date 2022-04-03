﻿using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IMessage
    {
        Guid MessageId { get; }
        Task Acknowledge();
        Task NotAcknowledge(string? reason = null);
        IEvent Content { get; } 
    }
}
