using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.Common.Events
{
  public interface IAnabasisExporterEvent: IEvent
  {
    Guid ExportId { get; }
    string Log();
  }
}
