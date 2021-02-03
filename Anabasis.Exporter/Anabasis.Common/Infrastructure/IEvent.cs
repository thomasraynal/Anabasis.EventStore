using System;

namespace Anabasis.Common.Infrastructure
{
  public interface IEvent
  {
    Guid EventID { get; }
    Guid CorrelationID { get; }
    string StreamId { get; }
    string TopicId { get; }
    string Log();
  }
}
