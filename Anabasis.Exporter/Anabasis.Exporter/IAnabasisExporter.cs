using Anabasis.Common;
using Anabasis.Common.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Exporter
{
  public interface IAnabasisExporter
  {
    Task ExportDocuments(StartExport startExport);
  }

}
