using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Importer;
using Lamar;

namespace Anabasis.App
{
  public class FileSystemRegistry : ServiceRegistry
  {
    public FileSystemRegistry()
    {
      Scan(scanner =>
      {
        scanner.AssembliesAndExecutablesFromApplicationBaseDirectory();
        scanner.AddAllTypesOf<IActor>();
        scanner.WithDefaultConventions();
      });

      var configuration = new FileSystemDocumentRepositoryConfiguration()
      {
        ClientId = "699173273524-aalbhs95og7ci38ink060v8bj166mej3.apps.googleusercontent.com",
        ClientSecret = "_9La2dPSNsZtRgo-0fUG00kV",
        DriveRootFolder = "1hOwSLQaBNWXnI5ik268O_mXPo3XHHBox",
        LocalDocumentFolder = @"E:\dev\anabasis\src\assets",
        RefreshToken = "1//03dpBmmfoO2X3CgYIARAAGAMSNwF-L9Ir9qcyc48kXirb_mr2yyPt8vnA4sJvSATu8EaScrKjb5-nyzw2uP69sP_EftPdrVy6YDE"
      };

      For<IAnabasisConfiguration>().Use(configuration);
      For<FileSystemDocumentRepositoryConfiguration>().Use(configuration);

    }
  }
}
