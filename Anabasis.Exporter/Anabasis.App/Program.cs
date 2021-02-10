using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Events.Commands;
using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using Anabasis.Exporter;
using Anabasis.Importer;
using System;
using System.Linq;
using System.Text;

namespace Anabasis.App
{
    class Program
    {
    static void Main(string[] args)
    {

      var mediator = World.Create<FileSystemRegistry>();

      mediator.Emit(new StartExportCommand(Guid.NewGuid(), StreamIds.GoogleDoc));

      Console.Read();

    }
  }
}
