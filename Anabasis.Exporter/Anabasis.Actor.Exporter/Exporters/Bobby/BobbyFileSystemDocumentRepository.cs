using Anabasis.EventStore;
using Anabasis.Importer;

namespace Anabasis.Common
{
  public class BobbyFileSystemDocumentRepository : BaseFileSystemDocumentRepository<JArrayExportFile>
  {
    public BobbyFileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, IEventStoreRepository eventStoreRepository) : base(configuration, eventStoreRepository)
    {
    }
    public override bool UseIndex => false;
  }
}
