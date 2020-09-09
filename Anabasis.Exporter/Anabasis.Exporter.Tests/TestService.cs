using System;
using System.Threading.Tasks;
using Xunit;

namespace Anabasis.Exporter.Tests
{
  public class TestService
  {
    [Fact]
    public async Task ShouldExportFolder()
    {
      var exporterConfiguration = new ExporterConfiguration()
      {
        ClientId = "699173273524-aalbhs95og7ci38ink060v8bj166mej3.apps.googleusercontent.com",
        ClientSecret = "_9La2dPSNsZtRgo-0fUG00kV",
        DriveRootFolder = "1e-fnRCTrPxpbo6Aq7-xiw3sQraFvQ-XM",
        LocalDocumentFolder = ".",
        RefreshToken = "1//03dpBmmfoO2X3CgYIARAAGAMSNwF-L9Ir9qcyc48kXirb_mr2yyPt8vnA4sJvSATu8EaScrKjb5-nyzw2uP69sP_EftPdrVy6YDE"
      };

      var documentService = new DocumentService(exporterConfiguration);


      await documentService.ExportFolder();


    }
  }
}
