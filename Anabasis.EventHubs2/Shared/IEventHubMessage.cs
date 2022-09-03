﻿using Anabasis.Common;

namespace Anabasis.EventHubs.Shared
{
    public interface IEventHubMessage: IMessage
    {
        bool IsAcknowledged { get; }
    }
}