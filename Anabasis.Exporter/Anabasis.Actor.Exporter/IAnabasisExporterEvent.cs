using Anabasis.EventStore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public interface IAnabasisExporterEvent: IEvent
  {
    Guid ExportId { get; }
  }
}
