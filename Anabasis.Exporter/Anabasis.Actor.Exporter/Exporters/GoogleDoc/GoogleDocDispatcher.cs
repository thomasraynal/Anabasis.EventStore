using Anabasis.Actor;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Exporter.GoogleDoc
{
  public class GoogleDocDispatcher : BaseActor
  {
    private readonly IAnabasisConfiguration _exporterConfiguration;
    private readonly GoogleDocClient _googleDocClient;

    public GoogleDocDispatcher(IAnabasisConfiguration exporterConfiguration, IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
      _exporterConfiguration = exporterConfiguration;
      _googleDocClient = new GoogleDocClient(exporterConfiguration);
    }

    private async Task SendExportRequest(RunExportCommand startExport, string folderId)
    {
      var nextUrl = $"https://www.googleapis.com/drive/v2/files/{folderId}/children";

      while (!string.IsNullOrEmpty(nextUrl))
      {
        var childList = await _googleDocClient.Get<ChildList>(nextUrl);

        var childReferences = childList.ChildReferences.Select(reference => reference.Id).ToArray();

        //only have one folder - but we should handle many and keep track of the original gdoc id
       await Emit(new ExportStarted(startExport.CorrelationID,
          childReferences,
          startExport.StreamId));

        foreach (var child in childList.ChildReferences)
        {

          await Emit(new ExportDocumentCommand(
            startExport.CorrelationID,
            startExport.StreamId,
            startExport.TopicId,
            child.Id,
            child.Id));

        }

        nextUrl = childList.NextLink;

      }

    }

    public async Task Handle(RunExportCommand startExportCommand)
    {
      await SendExportRequest(startExportCommand, _exporterConfiguration.DriveRootFolder);
    }


  }
}
