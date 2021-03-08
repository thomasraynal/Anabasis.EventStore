using Anabasis.Common;
using Anabasis.Importer;
using Lamar;
using Newtonsoft.Json;
using System.IO;

namespace Anabasis.App
{
  public class FileSystemRegistry : ServiceRegistry
  {
    public FileSystemRegistry()
    {
      //Scan(scanner =>
      //{
      //  scanner.AssembliesAndExecutablesFromApplicationBaseDirectory();
      //  scanner.AddAllTypesOf<IEvent>();
      //});

      var configuration = JsonConvert.DeserializeObject<FileSystemDocumentRepositoryConfiguration>(File.ReadAllText("ProdConfig.json"));

      For<IAnabasisConfiguration>().Use(configuration);
      For<FileSystemDocumentRepositoryConfiguration>().Use(configuration);

    }
  }
}
