using Anabasis.EventStore;
using Anabasis.Importer;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
  public class BobbyFileSystemDocumentRepository : BaseFileSystemDocumentRepository
  {
    public BobbyFileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, IEventStoreRepository eventStoreRepository) : base(configuration, eventStoreRepository)
    {
    }
    public override bool UseIndex => false;
  }
}
