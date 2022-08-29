using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Contracts
{
    public class NoMessageGroupStrategy : IMessageGroupStrategy
    {
        public int? BufferMaxSize => 1;

        public double? BufferAbsoluteTimeoutInSecond => 0.0;

        public double? BufferSlindingTimeoutInSecond => 0.0;

        public string GetEventGroupingKey(IEvent @event)
        {
            return $"{@event.EventId}";
        }
    }

    public interface IQueueBufferStrategy
    {
        int BufferMaxSize { get; }
        double BufferAbsoluteTimeoutInSecond { get; }
        double BufferSlindingTimeoutInSecond { get; }
    }

    public interface IMessageGroupStrategy : IQueueBufferStrategy
    {
        string GetEventGroupingKey(IEvent @event);
    }
}
