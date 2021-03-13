using Anabasis.EventStore;
using Anabasis.Importer;

namespace Anabasis.Common
{
  public class GoogleDocFileSystemDocumentRepository : BaseFileSystemDocumentRepository<JObjectExportFile>
  {
    public GoogleDocFileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, IEventStoreRepository eventStoreRepository) : base(configuration, eventStoreRepository)
    {
    }
    public override bool UseIndex => true;
  }
}
