using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Infrastructure
{
  public interface IStreamable
  {
    Guid EventID { get; }
    Guid CorrelationID { get; }
    string StreamId { get; }
    string TopicId { get; }
    string Log();
  }
}
