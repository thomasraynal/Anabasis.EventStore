using Lamar;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Integration.Tests
{
  public class TestRegistry : ServiceRegistry
  {
    public TestRegistry()
    {
      //Scan(scanner =>
      //{
      //  scanner.AssembliesAndExecutablesFromApplicationBaseDirectory();
      //  scanner.AddAllTypesOf<IEvent>();
      //});

      //var configuration = JsonConvert.DeserializeObject<FileSystemDocumentRepositoryConfiguration>(File.ReadAllText("ProdConfig.json"));

      //For<IAnabasisConfiguration>().Use(configuration);
      //For<FileSystemDocumentRepositoryConfiguration>().Use(configuration);

    }
  }
}
