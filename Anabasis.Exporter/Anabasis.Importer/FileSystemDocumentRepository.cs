using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public class Export
  {
    public ConcurrentBag<AnabasisDocument> AnabasisDocuments;
    public ConcurrentBag<DocumentIndex> DocumentIndices;

    public Export()
    {
      AnabasisDocuments = new ConcurrentBag<AnabasisDocument>();
      DocumentIndices = new ConcurrentBag<DocumentIndex>();
    }
  }

  public class FileSystemDocumentRepository : BaseDocumentRepository<FileSystemDocumentRepositoryConfiguration>
  {
    private readonly JsonSerializerSettings _jsonSerializerSettings;

    private readonly Dictionary<Guid, Export> _exports;

    public FileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, SimpleMediator simpleMediator) : base(configuration, simpleMediator)
    {

      _exports = new Dictionary<Guid, Export>();

      _jsonSerializerSettings = new JsonSerializerSettings
      {

        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy()
        },

        Formatting = Formatting.Indented

      };

    }

    public override Task OnExportStarted(Guid exportId)
    {

      _exports[exportId] = new Export();

      return Task.CompletedTask;
    }

    public override Task OnExportEnded(Guid exportId)
    {

      var export = _exports[exportId];

      var exportJson = JsonConvert.SerializeObject(export.AnabasisDocuments, Formatting.None, _jsonSerializerSettings);

      File.WriteAllText(Path.Combine(Configuration.LocalDocumentFolder, "export.json"), exportJson);

      var indexJson = JsonConvert.SerializeObject(export.DocumentIndices, Formatting.None, _jsonSerializerSettings);

      File.WriteAllText(Path.Combine(Configuration.LocalDocumentFolder, "index.json"), indexJson);

      return Task.CompletedTask;
    }

    public override Task SaveDocument(Guid exportId, AnabasisDocument anabasisDocument)
    {
      var export = _exports[exportId];

      export.AnabasisDocuments.Add(anabasisDocument);

      Mediator.Emit(new DocumentImported(anabasisDocument, exportId));

      return Task.CompletedTask;
    }

    public override Task SaveIndex(Guid exportId, DocumentIndex documentIndex)
    {
      var export = _exports[exportId];

      export.DocumentIndices.Add(documentIndex);

      Mediator.Emit(new IndexImported(documentIndex, exportId));

      return Task.CompletedTask;
    }
  }
}
