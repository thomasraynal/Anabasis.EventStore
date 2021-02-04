using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using Anabasis.Exporter;
using Anabasis.Importer;
using System;
using System.Text;

namespace Anabasis.App
{
    class Program
    {
    static void Main(string[] args)
    {

      var mediator = World.Create<FileSystemRegistry>();

      mediator.Emit(new StartExport(Guid.NewGuid(), StreamIds.Bobby));

      Console.Read();

    }
  }
}
