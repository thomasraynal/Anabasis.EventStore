using Anabasis.EventStore;
using Anabasis.EventStore.Repository;
using Anabasis.Importer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
  public class IlliadFileSystemDocumentRepository : BaseFileSystemDocumentRepository<JArrayExportFile>
  {
    public IlliadFileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, IEventStoreRepository eventStoreRepository) : base(configuration, eventStoreRepository)
    {
    }
    public override bool UseIndex => false;
  }
}
