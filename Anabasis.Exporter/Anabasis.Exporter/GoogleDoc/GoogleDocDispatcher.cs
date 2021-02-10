using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter.GoogleDoc
{
  public class GoogleDocDispatcher : BaseActor
  {
    private IAnabasisConfiguration _exporterConfiguration;
    private GoogleDocClient _googleDocClient;

    public override string StreamId => StreamIds.GoogleDoc;

    public GoogleDocDispatcher(IAnabasisConfiguration exporterConfiguration, IMediator simpleMediator) : base(simpleMediator)
    {
      _exporterConfiguration = exporterConfiguration;
      _googleDocClient = new GoogleDocClient(exporterConfiguration);
    }

    private async Task SendExportRequest(StartExportCommand startExport, string folderId)
    {
      var nextUrl = $"https://www.googleapis.com/drive/v2/files/{folderId}/children";

      while (!string.IsNullOrEmpty(nextUrl))
      {
        var childList = await _googleDocClient.Get<ChildList>(nextUrl);

        var childReferences = childList.ChildReferences.Select(reference => reference.Id).ToArray();

        //only have one folder - but we should handle many and keep track of the original gdoc id
        Mediator.Emit(new ExportStarted(startExport.CorrelationID,
          childReferences,
          startExport.StreamId,
          startExport.TopicId));

        foreach (var child in childList.ChildReferences)
        {
        
          Mediator.Emit(new ExportDocument(
            startExport.CorrelationID,
            startExport.StreamId,
            startExport.TopicId,
            child.Id,
            child.Id));

        }

        nextUrl = childList.NextLink;

      }

    }

    public async Task Handle(StartExportCommand startExportCommand)
    {
      await SendExportRequest(startExportCommand, _exporterConfiguration.DriveRootFolder);
    }


  }
}
