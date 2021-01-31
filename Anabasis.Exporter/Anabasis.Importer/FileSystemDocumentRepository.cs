using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Anabasis.Importer
{
  public class ExportFile: IDisposable
  {
    private readonly StreamWriter _streamWriter;
    private readonly JsonTextWriter _jsonTextWriter;
    private readonly JsonSerializer _jsonSerializer;

    public ExportFile(string path)
    {
      _streamWriter = File.CreateText(path);
      _jsonTextWriter = new JsonTextWriter(_streamWriter);

      _jsonSerializer = new JsonSerializer()
      {
        ContractResolver = new DefaultContractResolver
        {
          NamingStrategy = new CamelCaseNamingStrategy()
        },

        Formatting = Formatting.Indented

      };
    }

    public void StartWriting()
    {
      _jsonTextWriter.WriteStartArray();
    }

    public void EndWriting()
    {
      _jsonTextWriter.WriteEndArray();
    }

    public void Append(object item)
    {
      var jObject = JObject.FromObject(item, _jsonSerializer);

      jObject.WriteTo(_jsonTextWriter);
    }

    public void Dispose()
    {
      _streamWriter.Dispose();
      _jsonTextWriter.Close();
    }
  }


  public class Export : IDisposable
  {
    public ExportFile Documents { get; }
    public ExportFile Indices { get; }
    public string[] ExpectedDocumentIds { get; }
    public bool HasExportEnded { get; set; }
    public int ImportedDocumentCount { get; set; }
    public int ImportedIndicesCount { get; set; }
    public Export(string documentPath, string indicesPath, string[] documentIds)
    {
      Documents = new ExportFile(documentPath);
      Indices = new ExportFile(indicesPath);
      ExpectedDocumentIds = documentIds;
    }

    public void Dispose()
    {
      Documents.Dispose();
      Indices.Dispose();
    }
  }

  public class FileSystemDocumentRepository : BaseDocumentRepository<FileSystemDocumentRepositoryConfiguration>
  {

    private readonly Dictionary<Guid, Export> _exports;

    public FileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, SimpleMediator simpleMediator) : base(configuration, simpleMediator)
    {
      _exports = new Dictionary<Guid, Export>();
    }

    public override Task OnExportStarted(ExportStarted exportStarted)
    {
      var export = _exports[exportStarted.CorrelationID] = new Export(
        Path.Combine(Configuration.LocalDocumentFolder, "export.json"),
        Path.Combine(Configuration.LocalDocumentFolder, "index.json"),
        exportStarted.DocumentsIds);

      export.Documents.StartWriting();
      export.Indices.StartWriting();

      return Task.CompletedTask;
    }

    private void TryCompleteExport(Guid exportId)
    {
      var export = _exports[exportId];

      if (export.HasExportEnded &&
        export.ImportedDocumentCount == export.ExpectedDocumentIds.Length &&
        export.ImportedIndicesCount == export.ExpectedDocumentIds.Length)
      {
        export.Documents.EndWriting();
        export.Indices.EndWriting();

        export.Dispose();

        Mediator.Emit(new ExportEnded(exportId));

      }


    }

    public override Task OnExportEnd(Guid exportId)
    {
      
      var export = _exports[exportId];

      export.HasExportEnded = true;

      TryCompleteExport(exportId);

      return Task.CompletedTask;

    }

    public override Task SaveDocument(Guid exportId, AnabasisDocument anabasisDocument)
    {
      var export = _exports[exportId];

      export.Documents.Append(anabasisDocument);

      export.ImportedDocumentCount++;

      Mediator.Emit(new DocumentImported(anabasisDocument, exportId));

      TryCompleteExport(exportId);

      return Task.CompletedTask;
    }

    public override Task SaveIndex(Guid exportId, DocumentIndex documentIndex)
    {
      var export = _exports[exportId];

      export.Indices.Append(documentIndex);

      export.ImportedIndicesCount++;

      Mediator.Emit(new IndexImported(documentIndex, exportId));

      TryCompleteExport(exportId);

      return Task.CompletedTask;
    }
  }
}
