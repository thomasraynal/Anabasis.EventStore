using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Events.Commands;
using Anabasis.Common.Infrastructure;
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

    private bool _isDisposed;

    private readonly object _syncLock = new object();

    public ExportFile(string path)
    {
      var directory = Path.GetDirectoryName(path);

      Directory.CreateDirectory(directory);

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
      lock (_syncLock)
      {
        if (_isDisposed) return;
        _jsonTextWriter.WriteStartArray();
      }

    }

    public void EndWriting()
    {
      lock (_syncLock)
      {
        if (_isDisposed) return;
        _jsonTextWriter.WriteEndArray();

      }
    }

    public void Append(object item)
    {
      var jObject = JObject.FromObject(item, _jsonSerializer);

      jObject.WriteTo(_jsonTextWriter);
    }

    public void Dispose()
    {
      lock (_syncLock)
      {

        if (_isDisposed) return;

        _isDisposed = true;

        _streamWriter.Dispose();
        _jsonTextWriter.Close();

      }

     
    }
  }


  public class Export : IDisposable
  {
    public ExportFile Documents { get; }
    public ExportFile Indices { get; }
    public string[] ExpectedDocumentIds { get; }
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
    private readonly Dictionary<Guid, ICommand> _exportCallers;

    public override string StreamId => StreamIds.AllStream;

    public FileSystemDocumentRepository(FileSystemDocumentRepositoryConfiguration configuration, IMediator simpleMediator) : base(configuration, simpleMediator)
    {
      _exports = new Dictionary<Guid, Export>();
      _exportCallers = new Dictionary<Guid, ICommand>();
    }
    public override Task Handle(StartExportCommand startExportRequest)
    {
      _exportCallers.Add(startExportRequest.ExportId, startExportRequest);

      return Task.CompletedTask;
    }

    public override Task Handle(ExportStarted exportStarted)
    {
      var export = _exports[exportStarted.CorrelationID] = new Export(
        Path.Combine(Configuration.LocalDocumentFolder, exportStarted.StreamId, "export.json"),
        Path.Combine(Configuration.LocalDocumentFolder, exportStarted.StreamId, "index.json"),
        exportStarted.DocumentsIds);

      export.Documents.StartWriting();
      export.Indices.StartWriting();

      return Task.CompletedTask;
    }

    private void TryCompleteExport(IEvent exportEnded)
    {
      var export = _exports[exportEnded.CorrelationID];

      if (export.ImportedDocumentCount == export.ExpectedDocumentIds.Length &&
        export.ImportedIndicesCount == export.ExpectedDocumentIds.Length)
      {
        export.Documents.EndWriting();
        export.Indices.EndWriting();

        export.Dispose();

        var exportCommand = _exportCallers[exportEnded.CorrelationID];

        Mediator.Emit(new StartExportCommandResponse(exportCommand.EventID, exportEnded.CorrelationID, exportEnded.StreamId, exportEnded.TopicId));

      }
    }

    public override Task Handle(DocumentCreated documentCreated)
    {
      var export = _exports[documentCreated.ExportId];

      AnabasisDocument anabasisDocument = null;

      if (documentCreated.DocumentUri.IsFile)
      {
        anabasisDocument = JsonConvert.DeserializeObject<AnabasisDocument>(File.ReadAllText(documentCreated.DocumentUri.AbsolutePath));
      }

      //work out double deserialization
      export.Documents.Append(anabasisDocument);

      export.ImportedDocumentCount++;

      Mediator.Emit(new DocumentImported(anabasisDocument.Id, documentCreated.CorrelationID, documentCreated.StreamId, documentCreated.TopicId));

      TryCompleteExport(documentCreated);

      return Task.CompletedTask;
    }

    public override Task Handle(IndexCreated indexCreated)
    {
      var export = _exports[indexCreated.ExportId];

      AnabasisDocumentIndex anabasisDocumentIndex = null;

      if (indexCreated.AnabasisDocumentIndexUri.IsFile)
      {
        anabasisDocumentIndex = JsonConvert.DeserializeObject<AnabasisDocumentIndex>(File.ReadAllText(indexCreated.AnabasisDocumentIndexUri.AbsolutePath));
      }
      //work out double deserialization
      export.Indices.Append(anabasisDocumentIndex);

      export.ImportedIndicesCount++;

      Mediator.Emit(new IndexImported(indexCreated.DocumentId, indexCreated.CorrelationID, indexCreated.StreamId, indexCreated.TopicId));

      TryCompleteExport(indexCreated);

      return Task.CompletedTask;
    }
  }
}
